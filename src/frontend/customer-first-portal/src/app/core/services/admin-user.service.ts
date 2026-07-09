import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminUser {
  adminUserId: number;
  entraObjectId: string;
  email: string;
  displayName: string;
  department: string | null;
  canReview: boolean;
  receiveNotifications: boolean;
  isActive: boolean;
  createdUtc: string;
  updatedUtc: string;
}

export interface AddAdminUserRequest {
  entraObjectId: string;
  email: string;
  displayName: string;
  department: string | null;
  canReview: boolean;
  receiveNotifications: boolean;
}

export interface UpdateAdminUserRequest {
  canReview: boolean;
  receiveNotifications: boolean;
  isActive: boolean;
  department: string | null;
}

export interface DirectoryUser {
  entraObjectId: string;
  displayName: string;
  email: string;
  department: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class AdminUserService {
  private readonly apiUrl = `${environment.apiUrl}/admin-users`;

  constructor(private http: HttpClient) {}

  /**
   * Get list of all admin users
   */
  listAdminUsers(isActive?: boolean): Observable<AdminUser[]> {
    let params = '';
    if (isActive !== undefined) {
      params = `?isActive=${isActive}`;
    }
    return this.http.get<AdminUser[]>(`${this.apiUrl}${params}`);
  }

  /**
   * Add a new admin user
   */
  addAdminUser(request: AddAdminUserRequest): Observable<AdminUser> {
    return this.http.post<AdminUser>(this.apiUrl, request);
  }

  /**
   * Search Entra users via backend Microsoft Graph proxy
   */
  searchDirectoryUsers(query: string): Observable<DirectoryUser[]> {
    const encodedQuery = encodeURIComponent(query);
    return this.http.get<DirectoryUser[]>(
      `${this.apiUrl}/directory-search?query=${encodedQuery}`,
    );
  }

  /**
   * Remove an admin user
   */
  removeAdminUser(adminUserId: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/${adminUserId}`);
  }

  /**
   * Update admin user permissions/status
   */
  updateAdminUser(
    adminUserId: number,
    request: UpdateAdminUserRequest,
  ): Observable<AdminUser> {
    return this.http.put<AdminUser>(`${this.apiUrl}/${adminUserId}`, request);
  }
}
