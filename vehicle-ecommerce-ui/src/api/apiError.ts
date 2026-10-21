export interface ApiError {
  statusCode: number;
  message: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
}

export function getApiErrorMessage(error: unknown): string {
  if (
    typeof error === "object" &&
    error !== null &&
    "response" in error
  ) {
    const response = (error as {
      response?: { data?: ApiError };
    }).response;
    return response?.data?.message ?? "Request failed.";
  }
  return "Unable to connect to the server.";
}
