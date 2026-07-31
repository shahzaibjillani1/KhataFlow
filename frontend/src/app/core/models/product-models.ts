export interface Product {
  id: string;
  productName: string;
  productNameUr: string | null;
  categoryName: string | null;
  categoryNameUr: string | null;
  price: number;
  stock: number;
  inventoryStatus: number;
}

export interface ProductAddRequest {
  productName: string;
  categoryId: string;
  price: number;
  stock: number;
  inventoryStatus: number;
}

export interface ProductUpdateRequest extends ProductAddRequest {
  id: string;
}