import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProductService, Product } from '../../services/product.service';
import { CategoryService, Category } from '../../services/category.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './products.component.html',
  styleUrls: ['./products.component.css']
})
export class ProductsComponent implements OnInit {
  products: Product[] = [];
  categories: Category[] = [];
  loading = false;
  submitting = false;
  errorMessage = '';
  successMessage = '';

  // Filter States
  searchText = '';
  selectedCategoryId: number | null = null;

  // Form State
  isEditing = false;
  currentProductId: number | null = null;
  formModel: Product = {
    name: '',
    sku: '',
    barcode: '',
    description: '',
    price: 0,
    costPrice: 0,
    stockQuantity: 0,
    minStockLevel: 0,
    categoryId: 0,
    imageUrl: ''
  };

  // Image suggestions
  imagePlaceholders = [
    { name: 'Espresso', url: 'https://images.unsplash.com/photo-151097252790b-a481d6d7e9f9?w=300' },
    { name: 'Coffee Cup', url: 'https://images.unsplash.com/photo-1517701604599-bb29b565090c?w=300' },
    { name: 'Sandwich', url: 'https://images.unsplash.com/photo-1521390188846-e2a3a97453a0?w=300' },
    { name: 'Fries', url: 'https://images.unsplash.com/photo-1573080496219-bb080dd4f877?w=300' },
    { name: 'Brownie', url: 'https://images.unsplash.com/photo-1606313564200-e75d5e30476c?w=300' },
    { name: 'Cheesecake', url: 'https://images.unsplash.com/photo-1524351199679-46cddf530c04?w=300' }
  ];

  constructor(
    private productService: ProductService,
    private categoryService: CategoryService,
    public authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCategories();
    this.loadProducts();
  }

  loadCategories(): void {
    const user = this.authService.currentUserValue;
    const restaurantId = user && user.restaurantId ? Number(user.restaurantId) : undefined;

    this.categoryService.getCategories(restaurantId).subscribe({
      next: (data) => {
        this.categories = data;
        // If there is at least one category, set it as default in form
        if (data.length > 0 && !this.isEditing) {
          this.formModel.categoryId = data[0].id || 0;
        }
      }
    });
  }

  loadProducts(): void {
    this.loading = true;
    const user = this.authService.currentUserValue;
    const restaurantId = user && user.restaurantId ? Number(user.restaurantId) : undefined;

    this.productService.getProducts(this.searchText, this.selectedCategoryId || undefined, restaurantId).subscribe({
      next: (data) => {
        this.products = data;
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'Failed to load products. Please check server connection.';
        this.loading = false;
      }
    });
  }

  onFilterChange(): void {
    this.loadProducts();
  }

  clearFilters(): void {
    this.searchText = '';
    this.selectedCategoryId = null;
    this.loadProducts();
  }

  selectImagePreset(url: string): void {
    this.formModel.imageUrl = url;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.formModel.imageUrl = e.target.result;
      };
      reader.readAsDataURL(file);
    }
  }

  onSubmit(): void {
    if (!this.formModel.name.trim() || !this.formModel.sku.trim() || this.formModel.categoryId === 0) {
      this.errorMessage = 'Please fill out all required fields (Name, SKU, Category).';
      return;
    }

    const user = this.authService.currentUserValue;
    if (user && user.restaurantId) {
      this.formModel.restaurantId = Number(user.restaurantId);
    } else {
      this.errorMessage = 'A valid restaurant is required to manage products.';
      return;
    }

    this.submitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    // Automatically set barcode to SKU if empty
    if (!this.formModel.barcode?.trim()) {
      this.formModel.barcode = this.formModel.sku;
    }

    if (this.isEditing && this.currentProductId !== null) {
      this.productService.updateProduct(this.currentProductId, this.formModel).subscribe({
        next: () => {
          this.submitting = false;
          this.successMessage = `Product "${this.formModel.name}" updated successfully!`;
          this.resetForm();
          this.loadProducts();
        },
        error: (err) => {
          this.submitting = false;
          this.errorMessage = err.error || 'Failed to update product.';
        }
      });
    } else {
      this.productService.createProduct(this.formModel).subscribe({
        next: (newProd) => {
          this.submitting = false;
          this.successMessage = `Product "${newProd.name}" added successfully!`;
          this.resetForm();
          this.loadProducts();
        },
        error: (err) => {
          this.submitting = false;
          this.errorMessage = err.error || 'Failed to add product.';
        }
      });
    }
  }

  editProduct(product: Product): void {
    this.isEditing = true;
    this.currentProductId = product.id || null;
    this.formModel = {
      id: product.id,
      name: product.name,
      sku: product.sku,
      barcode: product.barcode || '',
      description: product.description || '',
      price: product.price,
      costPrice: product.costPrice,
      stockQuantity: product.stockQuantity,
      minStockLevel: product.minStockLevel,
      categoryId: product.categoryId,
      imageUrl: product.imageUrl || ''
    };
    this.errorMessage = '';
    this.successMessage = '';
  }

  deleteProduct(id: number, name: string): void {
    if (confirm(`Are you sure you want to delete product "${name}"?`)) {
      this.productService.deleteProduct(id).subscribe({
        next: () => {
          this.successMessage = `Product "${name}" deleted successfully.`;
          this.loadProducts();
          if (this.isEditing && this.currentProductId === id) {
            this.resetForm();
          }
        },
        error: (err) => {
          this.errorMessage = err.error || 'Failed to delete product.';
        }
      });
    }
  }

  resetForm(): void {
    this.isEditing = false;
    this.currentProductId = null;
    this.formModel = {
      name: '',
      sku: '',
      barcode: '',
      description: '',
      price: 0,
      costPrice: 0,
      stockQuantity: 0,
      minStockLevel: 0,
      categoryId: this.categories.length > 0 ? (this.categories[0].id || 0) : 0,
      imageUrl: ''
    };
  }

  isLowStock(prod: Product): boolean {
    return prod.stockQuantity <= prod.minStockLevel && prod.stockQuantity > 0;
  }

  isOutOfStock(prod: Product): boolean {
    return prod.stockQuantity === 0;
  }

  goBack(): void {
    this.router.navigate(['/']);
  }
}
