import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, User } from '../../services/auth.service';
import { RestaurantService } from '../../services/restaurant.service';

@Component({
  selector: 'app-super-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './super-admin-dashboard.component.html',
  styleUrls: ['./super-admin-dashboard.component.css']
})
export class SuperAdminDashboardComponent implements OnInit {
  stats: any = null;
  loadingStats = false;
  submittingOwner = false;
  successMessage = '';
  errorMessage = '';

  // Owner creation form state
  newOwner = {
    username: '',
    password: '',
    fullName: '',
    role: 'Owner' // Pre-locked role!
  };

  registeredOwners: User[] = [];
  loadingOwners = false;

  constructor(
    public authService: AuthService,
    private restaurantService: RestaurantService,
    private router: Router
  ) {
    if (!this.authService.isSuperAdmin) {
      this.router.navigate(['/']);
    }
  }

  ngOnInit(): void {
    this.loadStats();
    this.loadOwners();
  }

  loadStats(): void {
    this.loadingStats = true;
    this.restaurantService.getSuperAdminStats().subscribe({
      next: (res) => {
        this.stats = res;
        this.loadingStats = false;
      },
      error: () => {
        this.loadingStats = false;
      }
    });
  }

  loadOwners(): void {
    this.loadingOwners = true;
    // Fetch registered owners (users added by superadmin, who is ID 1)
    this.authService.getUsersByOwner(1).subscribe({
      next: (res) => {
        this.registeredOwners = res;
        this.loadingOwners = false;
      },
      error: () => {
        this.loadingOwners = false;
      }
    });
  }

  onSubmitOwner(): void {
    if (!this.newOwner.username || !this.newOwner.password || !this.newOwner.fullName) {
      this.errorMessage = 'Please fill out all owner fields.';
      return;
    }

    this.submittingOwner = true;
    this.errorMessage = '';
    this.successMessage = '';

    const payload = {
      ...this.newOwner,
      parentOwnerId: this.authService.currentUserValue?.id // set superadmin as parent
    };

    this.authService.registerUser(payload).subscribe({
      next: (res) => {
        this.submittingOwner = false;
        this.successMessage = `Restaurant Owner "${res.fullName}" registered successfully!`;
        this.resetOwnerForm();
        this.loadOwners();
        this.loadStats();
      },
      error: (err) => {
        this.submittingOwner = false;
        this.errorMessage = err.error || 'Failed to register owner.';
      }
    });
  }

  resetOwnerForm(): void {
    this.newOwner = {
      username: '',
      password: '',
      fullName: '',
      role: 'Owner'
    };
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
