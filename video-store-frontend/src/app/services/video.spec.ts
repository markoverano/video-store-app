import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpEventType, HttpResponse } from '@angular/common/http';

import { VideoService } from './video';
import { Video, VideoUploadResponse } from '../models';

describe('VideoService', () => {
  let service: VideoService;
  let httpMock: HttpTestingController;

  const mockVideos: Video[] = [
    {
      id: 1,
      title: 'Test Video 1',
      description: 'Test description 1',
      filePath: '/videos/test1.mp4',
      thumbnailPath: '/thumbnails/test1.jpg',
      thumbnailUrl: '/api/thumbnails/test1.jpg',
      createdDate: new Date(),
      categories: [{ id: 1, name: 'Action' }]
    },
    {
      id: 2,
      title: 'Test Video 2',
      description: 'Test description 2',
      filePath: '/videos/test2.mp4',
      thumbnailPath: '/thumbnails/test2.jpg',
      thumbnailUrl: '/api/thumbnails/test2.jpg',
      createdDate: new Date(),
      categories: [{ id: 2, name: 'Comedy' }]
    }
  ];

  const mockUploadResponse: VideoUploadResponse = {
    id: 3,
    title: 'Uploaded Video',
    message: 'Video uploaded successfully',
    thumbnailUrl: '/api/thumbnails/uploaded.jpg'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [VideoService]
    });

    service = TestBed.inject(VideoService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getVideos', () => {
    it('should return an Observable of Video array', () => {
      service.getVideos().subscribe(videos => {
        expect(videos.length).toBe(2);
        expect(videos).toEqual(mockVideos);
      });

      const request = httpMock.expectOne('/videos');
      expect(request.request.method).toBe('GET');
      request.flush(mockVideos);
    });

    it('should handle error when API fails', () => {
      service.getVideos().subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          expect(error.message).toBe('Server error. Please try again later.');
          expect(error.status).toBe(500);
        }
      });

      const request = httpMock.expectOne('/videos');
      request.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('should handle connection error', () => {
      service.getVideos().subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          expect(error.message).toBe('Unable to connect to the server. Please check your connection.');
        }
      });

      const request = httpMock.expectOne('/videos');
      request.error(new ProgressEvent('error'), { status: 0 });
    });
  });

  describe('getVideoById', () => {
    it('should return a single video', () => {
      const expectedVideo = mockVideos[0];

      service.getVideoById(1).subscribe(video => {
        expect(video).toEqual(expectedVideo);
        expect(video.id).toBe(1);
      });

      const request = httpMock.expectOne('/videos/1');
      expect(request.request.method).toBe('GET');
      request.flush(expectedVideo);
    });

    it('should handle 404 when video not found', () => {
      service.getVideoById(999).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          expect(error.message).toBe('The requested video was not found.');
          expect(error.status).toBe(404);
        }
      });

      const request = httpMock.expectOne('/videos/999');
      request.flush('Not found', { status: 404, statusText: 'Not Found' });
    });
  });

  describe('uploadVideo', () => {
    it('should upload video with FormData', () => {
      const testFile = new File(['test content'], 'test.mp4', { type: 'video/mp4' });
      const title = 'Test Upload';
      const description = 'Test description';
      const categoryIds = [1, 2];

      let uploadComplete = false;

      service.uploadVideo(title, description, categoryIds, testFile).subscribe(event => {
        if (event.type === HttpEventType.Response) {
          uploadComplete = true;
          const response = event as HttpResponse<VideoUploadResponse>;
          expect(response.body).toEqual(mockUploadResponse);
        }
      });

      const request = httpMock.expectOne('/videos');
      expect(request.request.method).toBe('POST');
      expect(request.request.body instanceof FormData).toBe(true);

      const formData = request.request.body as FormData;
      expect(formData.get('title')).toBe(title);
      expect(formData.get('description')).toBe(description);
      expect(formData.getAll('categoryIds')).toEqual(['1', '2']);
      expect(formData.get('file')).toBeTruthy();

      request.flush(mockUploadResponse);
      expect(uploadComplete).toBe(true);
    });

    it('should handle 413 payload too large error', () => {
      const testFile = new File(['test content'], 'large.mp4', { type: 'video/mp4' });

      service.uploadVideo('Test', 'Test', [1], testFile).subscribe({
        next: () => {},
        error: (error) => {
          expect(error.message).toBe('File size exceeds the maximum allowed limit of 100MB.');
          expect(error.status).toBe(413);
        }
      });

      const request = httpMock.expectOne('/videos');
      request.flush('Payload too large', { status: 413, statusText: 'Payload Too Large' });
    });

    it('should handle 415 unsupported media type error', () => {
      const testFile = new File(['test content'], 'test.txt', { type: 'text/plain' });

      service.uploadVideo('Test', 'Test', [1], testFile).subscribe({
        next: () => {},
        error: (error) => {
          expect(error.message).toBe('Unsupported file type. Please upload MP4, AVI, or MOV files only.');
          expect(error.status).toBe(415);
        }
      });

      const request = httpMock.expectOne('/videos');
      request.flush('Unsupported media type', { status: 415, statusText: 'Unsupported Media Type' });
    });

    it('should handle 400 bad request error with custom message', () => {
      const testFile = new File(['test content'], 'test.mp4', { type: 'video/mp4' });

      service.uploadVideo('', 'Test', [1], testFile).subscribe({
        next: () => {},
        error: (error) => {
          expect(error.message).toBe('Title is required');
          expect(error.status).toBe(400);
        }
      });

      const request = httpMock.expectOne('/videos');
      request.flush({ message: 'Title is required' }, { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('getVideoStreamUrl', () => {
    it('should return correct stream URL with API base', () => {
      const videoId = 1;

      const streamUrl = service.getVideoStreamUrl(videoId);

      expect(streamUrl).toContain('/videos/1/stream');
    });

    it('should return different URLs for different video IDs', () => {
      const url1 = service.getVideoStreamUrl(1);
      const url2 = service.getVideoStreamUrl(2);

      expect(url1).not.toBe(url2);
      expect(url1).toContain('/1/stream');
      expect(url2).toContain('/2/stream');
    });
  });
});
