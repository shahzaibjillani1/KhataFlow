import { UserRole } from "../enums/user-role";

export const UserRoleMap: Record<UserRole, number> = {
  SuperAdmin: 0,
  Owner: 1,
  Manager: 2,
  Staff: 3,
};

export interface StaffInviteFormValue {
  fullName: string;
  email: string;
  phoneNumber: string;
  role: UserRole;
}

export interface StaffInviteRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  role: number;
}

export interface InvitedUser {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  role: string;
  status: string;
}

export interface StaffInviteResponseData {
  user: InvitedUser;
  whatsAppShareUrl: string;
}