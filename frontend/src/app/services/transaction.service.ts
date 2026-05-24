import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product } from './product.service';

export interface TransactionItem {
  id?: number;
  productId: number;
  product?: Product;
  quantity: number;
  unitPrice?: number;
  costPrice?: number;
  subtotal?: number;
}

export interface Transaction {
  id?: number;
  transactionDate?: string;
  subtotal: number;
  tax: number;
  discount: number;
  total: number;
  paymentMethod: string;
  amountPaid: number;
  changeReturned?: number;
  status?: string;
  restaurantId?: number;
  cashierId?: number;
  transactionItems: TransactionItem[];
}

@Injectable({
  providedIn: 'root'
})
export class TransactionService {
  private apiUrl = 'http://localhost:5183/api/transactions';

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

  getTransactions(): Observable<Transaction[]> {
    return this.http.get<Transaction[]>(this.apiUrl);
  }

  createTransaction(transaction: Transaction): Observable<Transaction> {
    return this.http.post<Transaction>(this.apiUrl, transaction, this.getHeaders());
  }
}
