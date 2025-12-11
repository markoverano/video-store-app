import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { Video, VideoUploadResponse } from '../models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class VideoService {
  private readonly videosEndpoint = '/videos';

  constructor(private httpClient: HttpClient) {}

  getVideos(): Observable<Video[]> {
    this.logRequest('GET', this.videosEndpoint);
    return this.httpClient.get<Video[]>(this.videosEndpoint).pipe(
      tap(response => this.logResponse('GET', this.videosEndpoint, response)),
      catchError(error => this.handleError('getVideos', error))
    );
  }

  getVideoById(videoId: number): Observable<Video> {
    const endpoint = `${this.videosEndpoint}/${videoId}`;
    this.logRequest('GET', endpoint);
    return this.httpClient.get<Video>(endpoint).pipe(
      tap(response => this.logResponse('GET', endpoint, response)),
      catchError(error => this.handleError('getVideoById', error))
    );
  }

  uploadVideo(
    title: string,
    description: string,
    categoryIds: number[],
    videoFile: File
  ): Observable<HttpEvent<VideoUploadResponse>> {
    const formData = new FormData();
    formData.append('title', title);
    formData.append('description', description);
    categoryIds.forEach(categoryId => {
      formData.append('categoryIds', categoryId.toString());
    });
    formData.append('file', videoFile, videoFile.name);

    this.logRequest('POST', this.videosEndpoint, {
      title,
      description,
      categoryIds,
      fileName: videoFile.name,
      fileSize: videoFile.size
    });

    const uploadRequest = new HttpRequest<FormData>(
      'POST',
      this.videosEndpoint,
      formData,
      {
        reportProgress: true
      }
    );

    return this.httpClient.request<VideoUploadResponse>(uploadRequest).pipe(
      tap(event => this.logResponse('POST', this.videosEndpoint, event)),
      catchError(error => this.handleError('uploadVideo', error))
    );
  }

  getVideoStreamUrl(videoId: number): string {
    const streamUrl = `${environment.apiUrl}${this.videosEndpoint}/${videoId}/stream`;
    this.logRequest('STREAM', streamUrl);
    return streamUrl;
  }

  private logRequest(method: string, endpoint: string, payload?: unknown): void {
    if (!environment.production) {
      console.log(`[VideoService] ${method} Request: ${endpoint}`, payload ?? '');
    }
  }

  private logResponse(method: string, endpoint: string, response: unknown): void {
    if (!environment.production) {
      console.log(`[VideoService] ${method} Response from ${endpoint}:`, response);
    }
  }

  private handleError(operation: string, error: HttpErrorResponse): Observable<never> {
    let userFriendlyMessage = 'An unexpected error occurred. Please try again.';

    if (error.status === 0) {
      userFriendlyMessage = 'Unable to connect to the server. Please check your connection.';
    } else if (error.status === 400) {
      userFriendlyMessage = error.error?.message || 'Invalid request. Please check your input.';
    } else if (error.status === 413) {
      userFriendlyMessage = 'File size exceeds the maximum allowed limit of 100MB.';
    } else if (error.status === 415) {
      userFriendlyMessage = 'Unsupported file type. Please upload MP4, AVI, or MOV files only.';
    } else if (error.status === 404) {
      userFriendlyMessage = 'The requested video was not found.';
    } else if (error.status >= 500) {
      userFriendlyMessage = 'Server error. Please try again later.';
    }

    console.error(`[VideoService] ${operation} failed:`, {
      status: error.status,
      statusText: error.statusText,
      message: error.message,
      userFriendlyMessage
    });

    return throwError(() => ({
      originalError: error,
      message: userFriendlyMessage,
      status: error.status
    }));
  }
}
