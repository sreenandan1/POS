import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Category } from './category.service';

export interface Product {
  id?: number;
  name: string;
  sku: string;
  barcode?: string;
  description?: string;
  price: number;
  costPrice: number;
  stockQuantity: number;
  minStockLevel: number;
  categoryId: number;
  category?: Category;
  restaurantId?: number;
  imageUrl?: string;
  createdAt?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private apiUrl = 'http://localhost:5183/api/products';

  constructor(private http: HttpClient) {}

  private getHeaders() {
    const savedUser = localStorage.getItem('pos_user');
    let token = '';
    if (savedUser) {
      try {
        const user = JSON.parse(savedUser);
        token = user.token || '';
      } catch (e) {}
    }
    return {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    };
  }

  getProducts(search?: string, categoryId?: number, restaurantId?: number): Observable<Product[]> {
    let params = new HttpParams();
    if (search) {
      params = params.set('search', search);
    }
    if (categoryId) {
      params = params.set('categoryId', categoryId.toString());
    }
    if (restaurantId) {
      params = params.set('restaurantId', restaurantId.toString());
    }
    return this.http.get<Product[]>(this.apiUrl, { params });
  }

  getProduct(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }

  createProduct(product: Product): Observable<Product> {
    return this.http.post<Product>(this.apiUrl, product, this.getHeaders());
  }

  updateProduct(id: number, product: Product): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, product, this.getHeaders());
  }

  deleteProduct(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`, this.getHeaders());
  }
}
