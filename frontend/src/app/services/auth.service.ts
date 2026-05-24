import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';

export interface User {
  id: number;
  username: string;
  fullName: string;
  role: string;
  token?: string;
  restaurantId?: number;
  restaurantName?: string;
  createdAt?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'http://localhost:5183/api/auth';
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    const savedUser = localStorage.getItem('pos_user');
    if (savedUser) {
      try {
        this.currentUserSubject.next(JSON.parse(savedUser));
      } catch (e) {
        localStorage.removeItem('pos_user');
      }
    }
  }

  public get currentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  public get isAuthenticated(): boolean {
    return this.currentUserValue !== null;
  }

  public get isSuperAdmin(): boolean {
    return this.currentUserValue?.role === 'SuperAdmin';
  }

  public get isOwner(): boolean {
    return this.currentUserValue?.role === 'Owner';
  }

  public get isManager(): boolean {
    return this.currentUserValue?.role === 'Manager';
  }

  public get isStaff(): boolean {
    return this.currentUserValue?.role === 'Waiter' || this.currentUserValue?.role === 'Cashier';
  }

  login(credentials: any): Observable<User> {
    return this.http.post<User>(`${this.apiUrl}/login`, credentials).pipe(
      tap(user => {
        if (user && user.token) {
          localStorage.setItem('pos_user', JSON.stringify(user));
          this.currentUserSubject.next(user);
        }
      })
    );
  }

  // Public signup (logs the user in automatically)
  register(userData: any): Observable<User> {
    return this.http.post<User>(`${this.apiUrl}/register`, userData).pipe(
      tap(user => {
        if (user && user.token) {
          localStorage.setItem('pos_user', JSON.stringify(user));
          this.currentUserSubject.next(user);
        }
      })
    );
  }

  // Admin registration (Registers a staff user without overwriting the logged-in session!)
  registerUser(userData: any): Observable<User> {
    return this.http.post<User>(`${this.apiUrl}/register`, userData);
  }

  getUsersByOwner(ownerId: number): Observable<User[]> {
    return this.http.get<User[]>(`${this.apiUrl}/users-by-owner/${ownerId}`);
  }

  logout(): void {
    localStorage.removeItem('pos_user');
    this.currentUserSubject.next(null);
  }
}
