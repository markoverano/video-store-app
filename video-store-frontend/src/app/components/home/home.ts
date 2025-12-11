import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';
import { Video } from '../../models';
import { VideoService } from '../../services/video';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-home',
  standalone: false,
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit, OnDestroy {
  videos: Video[] = [];
  isLoading = false;
  errorMessage = '';
  private componentDestroyed$ = new Subject<void>();

  constructor(
    private videoService: VideoService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadVideos();
  }

  ngOnDestroy(): void {
    this.componentDestroyed$.next();
    this.componentDestroyed$.complete();
  }

  loadVideos(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.videoService.getVideos()
      .pipe(
        takeUntil(this.componentDestroyed$),
        finalize(() => this.isLoading = false)
      )
      .subscribe({
        next: (videos) => {
          this.videos = videos.map(video => ({
            ...video,
            thumbnailUrl: this.buildThumbnailUrl(video.thumbnailUrl)
          }));
        },
        error: (error) => {
          this.errorMessage = error.message || 'Failed to load videos. Please try again.';
        }
      });
  }

  private buildThumbnailUrl(relativePath: string): string {
    if (!relativePath) {
      return '';
    }
    const baseUrl = environment.apiUrl.replace('/api', '');
    return `${baseUrl}${relativePath}`;
  }

  navigateToStreaming(videoId: number): void {
    this.router.navigate(['/streaming', videoId]);
  }

  formatCategories(categories: { name: string }[]): string {
    if (!categories || categories.length === 0) {
      return 'Uncategorized';
    }
    return categories.map(category => category.name).join(', ');
  }
}
