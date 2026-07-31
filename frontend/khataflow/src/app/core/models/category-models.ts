export interface Category {
  id: string;
  categoryName: string;
  categoryNameUr: string | null;
}

export interface CategoryAddRequest {
  categoryName: string;
}

export interface CategoryUpdateRequest {
  id: string;
  categoryName: string;
}