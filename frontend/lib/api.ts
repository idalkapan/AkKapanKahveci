export class ApiError extends Error {
  constructor(message: string, public readonly status: number) {
    super(message);
  }
}
export async function request<T = unknown>(
  path: string,
  options?: RequestInit
): Promise<T> {
  const response = await fetch("/api/" + path, options);
  const data = await response.json().catch(() => ({}));
  if (!response.ok) {
    throw new ApiError(
      data.error ||
        (response.status === 429
          ? "Çok fazla deneme yaptınız. Bir dakika sonra tekrar deneyin."
          : "Bağlantı kurulamadı. Lütfen tekrar deneyin."),
      response.status
    );
  }
  return data;
}
