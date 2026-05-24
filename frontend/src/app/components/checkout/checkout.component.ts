import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProductService, Product } from '../../services/product.service';
import { CategoryService, Category } from '../../services/category.service';
import { TransactionService, Transaction, TransactionItem } from '../../services/transaction.service';
import { AuthService } from '../../services/auth.service';

interface CartItem {
  product: Product;
  quantity: number;
}

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './checkout.component.html',
  styleUrls: ['./checkout.component.css']
})
export class CheckoutComponent implements OnInit {
  products: Product[] = [];
  categories: Category[] = [];
  loading = false;

  // Search & Filter
  searchText = '';
  selectedCategoryId: number | null = null;

  // Cart State
  cart: CartItem[] = [];
  discount = 0;
  taxRate = 0.05; // 5% sales tax

  // Payment Modal State
  showPaymentModal = false;
  paymentMethod = 'Cash';
  amountPaid = 0;
  submittingCheckout = false;
  errorMessage = '';

  // Success Receipt State
  showReceiptModal = false;
  completedTransaction: Transaction | null = null;

  constructor(
    private productService: ProductService,
    private categoryService: CategoryService,
    private transactionService: TransactionService,
    public authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCategories();
    this.loadProducts();
  }

  loadCategories(): void {
    const restId = this.authService.currentUserValue?.restaurantId;
    this.categoryService.getCategories(restId || undefined).subscribe({
      next: (data) => {
        this.categories = data;
      }
    });
  }

  loadProducts(): void {
    this.loading = true;
    const restId = this.authService.currentUserValue?.restaurantId;
    this.productService.getProducts(this.searchText, this.selectedCategoryId || undefined, restId || undefined).subscribe({
      next: (data) => {
        this.products = data;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
      }
    });
  }

  filterByCategory(categoryId: number | null): void {
    this.selectedCategoryId = categoryId;
    this.loadProducts();
  }

  onSearchChange(): void {
    this.loadProducts();
  }

  // Cart Calculations
  get cartSubtotal(): number {
    return this.cart.reduce((sum, item) => sum + (item.product.price * item.quantity), 0);
  }

  get cartTax(): number {
    return Math.round(this.cartSubtotal * this.taxRate * 100) / 100;
  }

  get cartTotal(): number {
    const total = this.cartSubtotal + this.cartTax - this.discount;
    return total > 0 ? total : 0;
  }

  get cashChange(): number {
    const change = this.amountPaid - this.cartTotal;
    return change > 0 ? change : 0;
  }

  // Quick cash buttons
  get quickCashOptions(): number[] {
    const total = this.cartTotal;
    if (total === 0) return [10, 20, 50, 100];
    
    const exact = Math.ceil(total * 100) / 100;
    const options = [exact];

    // Find next rounding benchmarks
    const next5 = Math.ceil(total / 5) * 5;
    if (next5 > total && !options.includes(next5)) options.push(next5);

    const next10 = Math.ceil(total / 10) * 10;
    if (next10 > total && !options.includes(next10)) options.push(next10);

    const next20 = Math.ceil(total / 20) * 20;
    if (next20 > total && !options.includes(next20)) options.push(next20);

    const next50 = Math.ceil(total / 50) * 50;
    if (next50 > total && !options.includes(next50)) options.push(next50);

    return options.slice(0, 4); // return max 4 choices
  }

  // Cart Operations
  addToCart(product: Product): void {
    if (product.stockQuantity === 0) return; // Out of stock items are unclickable

    const existingIndex = this.cart.findIndex(item => item.product.id === product.id);
    
    if (existingIndex > -1) {
      const currentQty = this.cart[existingIndex].quantity;
      if (currentQty < product.stockQuantity) {
        this.cart[existingIndex].quantity++;
      } else {
        alert(`Cannot add more. Only ${product.stockQuantity} units available in stock.`);
      }
    } else {
      this.cart.push({ product, quantity: 1 });
    }
  }

  updateQuantity(item: CartItem, delta: number): void {
    const existingIndex = this.cart.findIndex(c => c.product.id === item.product.id);
    if (existingIndex > -1) {
      const newQty = this.cart[existingIndex].quantity + delta;
      if (newQty <= 0) {
        this.removeFromCart(item.product.id!);
      } else if (newQty <= item.product.stockQuantity) {
        this.cart[existingIndex].quantity = newQty;
      } else {
        alert(`Cannot add more. Only ${item.product.stockQuantity} units available in stock.`);
      }
    }
  }

  removeFromCart(productId: number): void {
    this.cart = this.cart.filter(item => item.product.id !== productId);
  }

  clearCart(): void {
    this.cart = [];
    this.discount = 0;
  }

  // Checkout Operations
  openPayment(): void {
    if (this.cart.length === 0) return;
    this.amountPaid = Math.ceil(this.cartTotal);
    this.errorMessage = '';
    this.showPaymentModal = true;
  }

  closePayment(): void {
    this.showPaymentModal = false;
  }

  selectQuickCash(amount: number): void {
    this.amountPaid = amount;
  }

  submitCheckout(): void {
    if (this.paymentMethod === 'Cash' && this.amountPaid < this.cartTotal) {
      this.errorMessage = 'Paid amount cannot be less than grand total due.';
      return;
    }

    this.submittingCheckout = true;
    this.errorMessage = '';

    // If card or mobile, set amountPaid to total
    const paidValue = this.paymentMethod === 'Cash' ? this.amountPaid : this.cartTotal;

    const transactionData: Transaction = {
      subtotal: this.cartSubtotal,
      tax: this.cartTax,
      discount: this.discount,
      total: this.cartTotal,
      paymentMethod: this.paymentMethod,
      amountPaid: paidValue,
      restaurantId: this.authService.currentUserValue?.restaurantId || 0,
      cashierId: this.authService.currentUserValue?.id || 0,
      transactionItems: this.cart.map(item => ({
        productId: item.product.id!,
        quantity: item.quantity
      }))
    };

    this.transactionService.createTransaction(transactionData).subscribe({
      next: (res) => {
        this.submittingCheckout = false;
        this.showPaymentModal = false;
        this.completedTransaction = res;
        this.showReceiptModal = true;
        this.clearCart();
        this.loadProducts(); // Reload catalog to reflect decremented stock levels!
      },
      error: (err) => {
        this.submittingCheckout = false;
        this.errorMessage = err.error || 'An error occurred during checkout processing.';
      }
    });
  }

  closeReceipt(): void {
    this.showReceiptModal = false;
    this.completedTransaction = null;
  }

  goBack(): void {
    this.router.navigate(['/']);
  }
}
