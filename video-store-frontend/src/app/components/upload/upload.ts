import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';
import { HttpEventType } from '@angular/common/http';
import { Category } from '../../models';
import { VideoService } from '../../services/video';
import { CategoryService } from '../../services/category';

@Component({
  selector: 'app-upload',
  standalone: false,
  templateUrl: './upload.html',
  styleUrl: './upload.css',
})
export class Upload implements OnInit, OnDestroy {
  uploadForm!: FormGroup;
  categories: Category[] = [];
  selectedCategoryIds: number[] = [];
  isUploading = false;
  uploadProgress = 0;
  errorMessage = '';
  successMessage = '';
  selectedFile: File | null = null;
  fileError = '';

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private videoService: VideoService,
    private categoryService: CategoryService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.uploadForm = this.fb.group({
      title: ['', Validators.required],
      description: ['']
    });
    this.loadCategories();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadCategories(): void {
    this.categoryService.getCategories()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (cats) => this.categories = cats,
        error: () => {}
      });
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    this.fileError = '';
    this.selectedFile = null;

    if (!file) return;

    const ext = file.name.split('.').pop()?.toLowerCase();
    if (!['mp4', 'avi', 'mov'].includes(ext || '')) {
      this.fileError = 'Only MP4, AVI, MOV files allowed';
      return;
    }

    if (file.size > 100 * 1024 * 1024) {
      this.fileError = 'File must be under 100MB';
      return;
    }

    this.selectedFile = file;
  }

  toggleCategory(id: number): void {
    const idx = this.selectedCategoryIds.indexOf(id);
    if (idx === -1) {
      this.selectedCategoryIds.push(id);
    } else {
      this.selectedCategoryIds.splice(idx, 1);
    }
  }

  onSubmit(): void {
    if (!this.uploadForm.valid || !this.selectedFile) {
      this.errorMessage = 'Please fill title and select a file';
      return;
    }

    this.isUploading = true;
    this.errorMessage = '';
    this.uploadProgress = 0;

    const { title, description } = this.uploadForm.value;

    this.videoService.uploadVideo(title, description || '', this.selectedCategoryIds, this.selectedFile)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.isUploading = false)
      )
      .subscribe({
        next: (event) => {
          if (event.type === HttpEventType.UploadProgress && event.total) {
            this.uploadProgress = Math.round((event.loaded / event.total) * 100);
          } else if (event.type === HttpEventType.Response) {
            this.successMessage = 'Upload successful!';
            const videoId = event.body?.id;
            if (videoId) {
              setTimeout(() => this.router.navigate(['/streaming', videoId]), 1000);
            }
          }
        },
        error: (err) => {
          this.errorMessage = err.message || 'Upload failed';
        }
      });
  }
}
