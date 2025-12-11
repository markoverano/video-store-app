import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';
import { Video } from '../../models';
import { VideoService } from '../../services/video';

@Component({
  selector: 'app-streaming',
  standalone: false,
  templateUrl: './streaming.html',
  styleUrl: './streaming.css',
})
export class Streaming implements OnInit, OnDestroy {
  video: Video | null = null;
  videoStreamUrl = '';
  isLoading = false;
  errorMessage = '';
  private componentDestroyed$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private videoService: VideoService
  ) {}

  ngOnInit(): void {
    const videoId = this.route.snapshot.paramMap.get('id');
    if (videoId) {
      this.loadVideo(+videoId);
    } else {
      this.errorMessage = 'No video ID provided.';
    }
  }

  ngOnDestroy(): void {
    this.componentDestroyed$.next();
    this.componentDestroyed$.complete();
  }

  loadVideo(videoId: number): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.videoService.getVideoById(videoId)
      .pipe(
        takeUntil(this.componentDestroyed$),
        finalize(() => this.isLoading = false)
      )
      .subscribe({
        next: (video) => {
          this.video = video;
          this.videoStreamUrl = this.videoService.getVideoStreamUrl(videoId);
        },
        error: (error) => {
          this.errorMessage = error.message || 'Failed to load video.';
        }
      });
  }

  navigateToHome(): void {
    this.router.navigate(['/home']);
  }

  formatCategories(categories: { name: string }[]): string {
    if (!categories || categories.length === 0) {
      return 'Uncategorized';
    }
    return categories.map(category => category.name).join(', ');
  }

  onVideoError(): void {
    this.errorMessage = 'Failed to load video. The video file may be unavailable.';
  }
}
