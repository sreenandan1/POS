import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, User } from '../../services/auth.service';
import { RestaurantService, Restaurant } from '../../services/restaurant.service';

@Component({
  selector: 'app-owner-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './owner-dashboard.component.html',
  styleUrls: ['./owner-dashboard.component.css']
})
export class OwnerDashboardComponent implements OnInit {
  stats: any = null;
  loadingStats = false;
  
  // Lists
  restaurants: Restaurant[] = [];
  managers: User[] = [];
  loadingRestaurants = false;
  loadingManagers = false;

  // Restaurant Creation Form
  newRestaurant = {
    name: '',
    address: '',
    phone: ''
  };
  submittingRestaurant = false;
  restSuccess = '';
  restError = '';

  // Manager Creation Form
  newManager = {
    username: '',
    password: '',
    fullName: '',
    restaurantId: 0
  };
  submittingManager = false;
  mgrSuccess = '';
  mgrError = '';

  constructor(
    public authService: AuthService,
    private restaurantService: RestaurantService,
    private router: Router
  ) {
    if (!this.authService.isOwner) {
      this.router.navigate(['/']);
    }
  }

  ngOnInit(): void {
    this.loadStats();
    this.loadRestaurants();
    this.loadManagers();
  }

  loadStats(): void {
    const ownerId = this.authService.currentUserValue?.id;
    if (!ownerId) return;

    this.loadingStats = true;
    this.restaurantService.getOwnerStats(ownerId).subscribe({
      next: (res) => {
        this.stats = res;
        this.loadingStats = false;
      },
      error: () => {
        this.loadingStats = false;
      }
    });
  }

  loadRestaurants(): void {
    const ownerId = this.authService.currentUserValue?.id;
    if (!ownerId) return;

    this.loadingRestaurants = true;
    this.restaurantService.getRestaurants(ownerId).subscribe({
      next: (res) => {
        this.restaurants = res;
        this.loadingRestaurants = false;
        if (res.length > 0) {
          this.newManager.restaurantId = res[0].id || 0;
        }
      },
      error: () => {
        this.loadingRestaurants = false;
      }
    });
  }

  loadManagers(): void {
    const ownerId = this.authService.currentUserValue?.id;
    if (!ownerId) return;

    this.loadingManagers = true;
    this.authService.getUsersByOwner(ownerId).subscribe({
      next: (res) => {
        // Filter users added by this owner who have the Manager role!
        this.managers = res.filter(u => u.role === 'Manager');
        this.loadingManagers = false;
      },
      error: () => {
        this.loadingManagers = false;
      }
    });
  }

  onSubmitRestaurant(): void {
    if (!this.newRestaurant.name.trim()) {
      this.restError = 'Restaurant name is required.';
      return;
    }

    this.submittingRestaurant = true;
    this.restSuccess = '';
    this.restError = '';

    const payload: Restaurant = {
      name: this.newRestaurant.name,
      address: this.newRestaurant.address,
      phone: this.newRestaurant.phone,
      ownerId: this.authService.currentUserValue?.id || 0
    };

    this.restaurantService.createRestaurant(payload).subscribe({
      next: () => {
        this.submittingRestaurant = false;
        this.restSuccess = `Restaurant "${payload.name}" added successfully!`;
        this.resetRestaurantForm();
        this.loadRestaurants();
        this.loadStats();
      },
      error: (err) => {
        this.submittingRestaurant = false;
        this.restError = err.error || 'Failed to add restaurant outlet.';
      }
    });
  }

  onSubmitManager(): void {
    if (!this.newManager.username || !this.newManager.password || !this.newManager.fullName || this.newManager.restaurantId === 0) {
      this.mgrError = 'Please fill out all manager profile fields and assign a branch.';
      return;
    }

    this.submittingManager = true;
    this.mgrSuccess = '';
    this.mgrError = '';

    const payload = {
      username: this.newManager.username,
      password: this.newManager.password,
      fullName: this.newManager.fullName,
      role: 'Manager',
      restaurantId: this.newManager.restaurantId,
      parentOwnerId: this.authService.currentUserValue?.id
    };

    this.authService.registerUser(payload).subscribe({
      next: (res) => {
        this.submittingManager = false;
        this.mgrSuccess = `Manager "${res.fullName}" registered successfully!`;
        this.resetManagerForm();
        this.loadManagers();
      },
      error: (err) => {
        this.submittingManager = false;
        this.mgrError = err.error || 'Failed to register manager.';
      }
    });
  }

  getRestaurantName(id?: number): string {
    if (!id) return 'Unassigned';
    const rest = this.restaurants.find(r => r.id === id);
    return rest ? rest.name : 'Unknown';
  }

  resetRestaurantForm(): void {
    this.newRestaurant = { name: '', address: '', phone: '' };
  }

  resetManagerForm(): void {
    this.newManager = {
      username: '',
      password: '',
      fullName: '',
      restaurantId: this.restaurants.length > 0 ? (this.restaurants[0].id || 0) : 0
    };
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
