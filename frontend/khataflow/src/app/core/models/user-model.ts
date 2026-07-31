import { Gender } from "../enums/gender";
import { UserPlan } from "../enums/user-plan";
import { UserRole } from "../enums/user-role";
import { UserStatus } from "../enums/user-status";

export interface User {
  id: string;
  fullName: string;
  fullNameUr: string | null;
  displayName: string | null;
  displayNameUr: string | null;
  email: string;
  phoneNumber: string | null;
  profilePictureUrl: string | null;
  businessId: string;
  gender: Gender;
  dateOfBirth: string | null;
  role: UserRole;
  status: UserStatus;
  plan: UserPlan;
  planExpiryDate: string | null;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface UserUpdateRequest {
  fullName?: string;
  displayName?: string;
  email?: string;
  phoneNumber?: string;
  profilePictureUrl?: string;
  gender?: Gender;
  dateOfBirth?: string;
}