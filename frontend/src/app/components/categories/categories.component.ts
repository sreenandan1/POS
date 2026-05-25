import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CategoryService, Category } from '../../services/category.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './categories.component.html',
  styleUrls: ['./categories.component.css']
})
export class CategoriesComponent implements OnInit {
  categories: Category[] = [];
  loading = false;
  submitting = false;
  errorMessage = '';
  successMessage = '';

  // Form State
  isEditing = false;
  currentCategoryId: number | null = null;
  formModel: Category = {
    name: '',
    color: '#0d6efd' // Default bootstrap primary
  };

  // Modern preset colors for POS screen aesthetics
  presetColors = [
    { name: 'Indigo Blue', value: '#0d6efd' },
    { name: 'Emerald Green', value: '#198754' },
    { name: 'Rose Red', value: '#dc3545' },
    { name: 'Amber Orange', value: '#ffc107' },
    { name: 'Cyan Teal', value: '#0dcaf0' },
    { name: 'Amethyst Purple', value: '#6f42c1' },
    { name: 'Hot Pink', value: '#d63384' },
    { name: 'Slate Gray', value: '#6c757d' }
  ];

  constructor(
    private categoryService: CategoryService,
    public authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading = true;
    const user = this.authService.currentUserValue;
    const restaurantId = user && user.restaurantId ? Number(user.restaurantId) : undefined;

    this.categoryService.getCategories(restaurantId).subscribe({
      next: (data) => {
        this.categories = data;
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'Failed to load categories. Please check your server connection.';
        this.loading = false;
      }
    });
  }

  selectPresetColor(color: string): void {
    this.formModel.color = color;
  }

  onSubmit(): void {
    if (!this.formModel.name.trim()) {
      this.errorMessage = 'Category name is required.';
      return;
    }

    const user = this.authService.currentUserValue;
    if (user && user.restaurantId) {
      this.formModel.restaurantId = Number(user.restaurantId);
    } else {
      this.errorMessage = 'A valid restaurant is required to manage categories.';
      return;
    }

    this.submitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    if (this.isEditing && this.currentCategoryId !== null) {
      this.categoryService.updateCategory(this.currentCategoryId, this.formModel).subscribe({
        next: () => {
          this.submitting = false;
          this.successMessage = `Category "${this.formModel.name}" updated successfully!`;
          this.resetForm();
          this.loadCategories();
        },
        error: (err) => {
          this.submitting = false;
          this.errorMessage = err.error || 'Failed to update category.';
        }
      });
    } else {
      this.categoryService.createCategory(this.formModel).subscribe({
        next: (newCat) => {
          this.submitting = false;
          this.successMessage = `Category "${newCat.name}" created successfully!`;
          this.resetForm();
          this.loadCategories();
        },
        error: (err) => {
          this.submitting = false;
          this.errorMessage = err.error || 'Failed to create category.';
        }
      });
    }
  }

  editCategory(category: Category): void {
    this.isEditing = true;
    this.currentCategoryId = category.id || null;
    this.formModel = {
      id: category.id,
      name: category.name,
      color: category.color
    };
    this.errorMessage = '';
    this.successMessage = '';
  }

  deleteCategory(id: number, name: string): void {
    if (confirm(`Are you sure you want to delete category "${name}"?`)) {
      this.categoryService.deleteCategory(id).subscribe({
        next: () => {
          this.successMessage = `Category "${name}" deleted successfully.`;
          this.loadCategories();
          if (this.isEditing && this.currentCategoryId === id) {
            this.resetForm();
          }
        },
        error: (err) => {
          this.errorMessage = err.error || 'Failed to delete category.';
        }
      });
    }
  }

  resetForm(): void {
    this.isEditing = false;
    this.currentCategoryId = null;
    this.formModel = {
      name: '',
      color: '#0d6efd'
    };
  }

  goBack(): void {
    this.router.navigate(['/']);
  }
}
