import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/auth-models';
import {
  StaffInviteFormValue,
  StaffInviteRequest,
  StaffInviteResponseData,
  UserRoleMap,
} from '../core/models/staff-invite-models';

@Injectable({ providedIn: 'root' })
export class StaffService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/v1/Users/staff`;

  inviteStaff(form: StaffInviteFormValue): Observable<ApiResponse<StaffInviteResponseData>> {
    const payload: StaffInviteRequest = {
      fullName: form.fullName,
      email: form.email,
      phoneNumber: form.phoneNumber,
      role: UserRoleMap[form.role],
    };
    return this.http.post<ApiResponse<StaffInviteResponseData>>(`${this.baseUrl}/invite`, payload);
  }
}