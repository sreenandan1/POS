import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Restaurant {
  id?: number;
  name: string;
  address?: string;
  phone?: string;
  ownerId: number;
  createdAt?: string;
}

@Injectable({
  providedIn: 'root'
})
export class RestaurantService {
  private apiUrl = 'http://localhost:5183/api/restaurants';
  private dashUrl = 'http://localhost:5183/api/dashboard';

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

  getRestaurants(ownerId?: number): Observable<Restaurant[]> {
    let params = new HttpParams();
    if (ownerId) {
      params = params.set('ownerId', ownerId.toString());
    }
    return this.http.get<Restaurant[]>(this.apiUrl, { params });
  }

  getRestaurant(id: number): Observable<Restaurant> {
    return this.http.get<Restaurant>(`${this.apiUrl}/${id}`);
  }

  createRestaurant(restaurant: Restaurant): Observable<Restaurant> {
    return this.http.post<Restaurant>(this.apiUrl, restaurant, this.getHeaders());
  }

  // Dashboard statistics APIs
  getSuperAdminStats(): Observable<any> {
    return this.http.get<any>(`${this.dashUrl}/superadmin`, this.getHeaders());
  }

  getOwnerStats(ownerId: number): Observable<any> {
    return this.http.get<any>(`${this.dashUrl}/owner/${ownerId}`, this.getHeaders());
  }

  getRestaurantStats(restaurantId: number): Observable<any> {
    return this.http.get<any>(`${this.dashUrl}/restaurant/${restaurantId}`, this.getHeaders());
  }
}
