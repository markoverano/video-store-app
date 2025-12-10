export interface Video {
  id: number;
  title: string;
  description: string;
  filePath: string;
  thumbnailPath: string;
  thumbnailUrl: string;
  createdDate: Date;
  categories: Category[];
}

export interface Category {
  id: number;
  name: string;
}

export interface VideoUploadRequest {
  title: string;
  description: string;
  categoryIds: number[];
  file: File;
}

export interface VideoUploadResponse {
  id: number;
  title: string;
  message: string;
  thumbnailUrl: string;
}
