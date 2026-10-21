export interface TokenResponse {
  accessToken: string;
  refreshToken?: string;
  expiresIn: number;
  userId: string;
  email: string;
  roles: string[];
}
