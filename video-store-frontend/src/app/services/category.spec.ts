import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import { CategoryService } from './category';
import { Category } from '../models';

describe('CategoryService', () => {
  let service: CategoryService;
  let httpMock: HttpTestingController;

  const mockCategories: Category[] = [
    { id: 1, name: 'Action' },
    { id: 2, name: 'Comedy' },
    { id: 3, name: 'Drama' }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [CategoryService]
    });

    service = TestBed.inject(CategoryService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getCategories', () => {
    it('should return an Observable of Category array', () => {
      service.getCategories().subscribe(categories => {
        expect(categories.length).toBe(3);
        expect(categories).toEqual(mockCategories);
      });

      const request = httpMock.expectOne('/categories');
      expect(request.request.method).toBe('GET');
      request.flush(mockCategories);
    });
  });

  describe('getCategoryById', () => {
    it('should return a single category', () => {
      const expectedCategory = mockCategories[0];

      service.getCategoryById(1).subscribe(category => {
        expect(category).toEqual(expectedCategory);
        expect(category.id).toBe(1);
      });

      const request = httpMock.expectOne('/categories/1');
      expect(request.request.method).toBe('GET');
      request.flush(expectedCategory);
    });
  });

  describe('createCategory', () => {
    it('should create a new category', () => {
      const newCategory: Category = { id: 4, name: 'Horror' };

      service.createCategory('Horror').subscribe(category => {
        expect(category).toEqual(newCategory);
        expect(category.name).toBe('Horror');
      });

      const request = httpMock.expectOne('/categories');
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toEqual({ name: 'Horror' });
      request.flush(newCategory);
    });
  });
});
