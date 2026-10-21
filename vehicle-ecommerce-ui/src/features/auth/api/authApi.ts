import apiClient from "../../../api/axiosClient";
import type { LoginRequest } from "../types/LoginRequest";
import type { TokenResponse } from "../types/TokenResponse";
import type { User } from "../types/User";

export async function login(
  request: LoginRequest
): Promise<TokenResponse> {
  const response = await apiClient.post<TokenResponse>(
    "/api/auth/login",
    request
  );
  return response.data;
}

export async function refresh(): Promise<TokenResponse> {
  const response = await apiClient.post<TokenResponse>(
    "/api/auth/refresh"
  );
  return response.data;
}

export async function logout(): Promise<void> {
  await apiClient.post("/api/auth/logout");
}

export async function getMe(): Promise<User> {
  const response = await apiClient.get<User>("/api/auth/me");
  return response.data;
}
