export type Product = {
  id: string;
  name: string;
  description: string;
  price: number;
  category: string;
  color: string;
  emoji: string;
  tag: string;
};
export type Line = {
  product: Product;
  quantity: number;
  customCup: boolean;
  cupText: string;
  cupColor: string;
};

export type User = { name: string; email: string };
export type StudentStatus =
  | "none"
  | "pending"
  | "approved"
  | "rejected"
  | "expired";
export type ApiResponses = {
  "auth/me": User;
  "auth/logout": { status: string };
  menu: Product[];
  student: { status: StudentStatus };
  checkout: {
    id: string;
    total: number;
    discount: number;
    status: string;
    message: string;
  };
};
