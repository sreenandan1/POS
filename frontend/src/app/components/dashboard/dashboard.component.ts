import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService, User } from '../../services/auth.service';
import { RestaurantService } from '../../services/restaurant.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  currentUser: any = null;
  stats: any = null;
  loadingStats = false;

  // Staff registration form state
  newStaff = {
    username: '',
    password: '',
    fullName: '',
    role: 'Waiter' // Waiter or Cashier
  };
  submittingStaff = false;
  staffSuccess = '';
  staffError = '';

  registeredStaff: User[] = [];
  loadingStaff = false;

  constructor(
    public authService: AuthService,
    private restaurantService: RestaurantService,
    private router: Router
  ) {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });

    // Managers only. Other roles redirected to their scoped spaces!
    if (this.authService.isSuperAdmin) {
      this.router.navigate(['/superadmin']);
    } else if (this.authService.isOwner) {
      this.router.navigate(['/owner']);
    } else if (this.authService.isStaff) {
      this.router.navigate(['/checkout']);
    }
  }

  ngOnInit(): void {
    this.loadRestaurantStats();
    this.loadStaffList();
  }

  loadRestaurantStats(): void {
    const restId = this.currentUser?.restaurantId;
    if (!restId) return;

    this.loadingStats = true;
    this.restaurantService.getRestaurantStats(restId).subscribe({
      next: (res) => {
        this.stats = res;
        this.loadingStats = false;
      },
      error: () => {
        this.loadingStats = false;
      }
    });
  }

  loadStaffList(): void {
    const managerId = this.currentUser?.id;
    if (!managerId) return;

    this.loadingStaff = true;
    // Fetch registered staff (users registered by this manager who are ID managerId)
    this.authService.getUsersByOwner(managerId).subscribe({
      next: (res) => {
        this.registeredStaff = res.filter(u => u.role === 'Waiter' || u.role === 'Cashier');
        this.loadingStaff = false;
      },
      error: () => {
        this.loadingStaff = false;
      }
    });
  }

  onSubmitStaff(): void {
    if (!this.newStaff.username || !this.newStaff.password || !this.newStaff.fullName) {
      this.staffError = 'Please fill out all staff fields.';
      return;
    }

    this.submittingStaff = true;
    this.staffSuccess = '';
    this.staffError = '';

    const payload = {
      ...this.newStaff,
      restaurantId: this.currentUser?.restaurantId, // Auto-scoped to the manager's restaurant!
      parentOwnerId: this.currentUser?.id // Map manager as register parent
    };

    this.authService.registerUser(payload).subscribe({
      next: (res) => {
        this.submittingStaff = false;
        this.staffSuccess = `Staff Member "${res.fullName}" registered successfully!`;
        this.resetStaffForm();
        this.loadStaffList();
        this.loadRestaurantStats(); // Reload to update any staff counts
      },
      error: (err) => {
        this.submittingStaff = false;
        this.staffError = err.error || 'Failed to register staff member.';
      }
    });
  }

  resetStaffForm(): void {
    this.newStaff = {
      username: '',
      password: '',
      fullName: '',
      role: 'Waiter'
    };
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
