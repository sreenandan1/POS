import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Category {
  id?: number;
  name: string;
  color: string;
  restaurantId?: number;
  createdAt?: string;
}

@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  private apiUrl = 'http://localhost:5183/api/categories';

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

  getCategories(restaurantId?: number): Observable<Category[]> {
    let params = new HttpParams();
    if (restaurantId) {
      params = params.set('restaurantId', restaurantId.toString());
    }
    return this.http.get<Category[]>(this.apiUrl, { params });
  }

  getCategory(id: number): Observable<Category> {
    return this.http.get<Category>(`${this.apiUrl}/${id}`);
  }

  createCategory(category: Category): Observable<Category> {
    return this.http.post<Category>(this.apiUrl, category, this.getHeaders());
  }

  updateCategory(id: number, category: Category): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, category, this.getHeaders());
  }

  deleteCategory(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`, this.getHeaders());
  }
}
