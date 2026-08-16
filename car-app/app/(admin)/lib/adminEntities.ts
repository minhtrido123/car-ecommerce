export type FieldType =
  | "text"
  | "email"
  | "password"
  | "number"
  | "decimal"
  | "checkbox"
  | "textarea"
  | "select";

export interface EntityField {
  key: string;
  label: string;
  type: FieldType;
  required?: boolean;
  showInTable?: boolean;
  options?: { value: string; label: string }[];
  lookup?: { route: string; valueKey: string; labelKey: string };
  hiddenOnCreate?: boolean;
  hiddenOnEdit?: boolean;
}

export interface EntityConfig {
  slug: string;
  route: string;
  label: string;
  singular?: string;
  userAuth?: boolean;
  canCreate?: boolean;
  canEdit?: boolean;
  canDelete?: boolean;
  images?: boolean;
  fields: EntityField[];
}

const statusOptions = [
  { value: "Active", label: "Active" },
  { value: "Inactive", label: "Inactive" },
  { value: "Sold", label: "Sold" },
];

export const adminEntities: Record<string, EntityConfig> = {
  users: {
    slug: "users",
    route: "users",
    label: "Users",
    singular: "User",
    userAuth: true,
    fields: [
      { key: "email", label: "Email", type: "email", required: true },
      { key: "password", label: "Password", type: "password", showInTable: false },
      { key: "name", label: "Name", type: "text", required: true },
      {
        key: "role",
        label: "Role",
        type: "select",
        required: true,
        options: [
          { value: "Admin", label: "Admin" },
          { value: "Staff", label: "Staff" },
          { value: "Customer", label: "Customer" },
        ],
      },
      { key: "createdAt", label: "Created", type: "text", hiddenOnCreate: true, hiddenOnEdit: true },
    ],
  },
  brands: {
    slug: "brands",
    route: "brands",
    label: "Brands",
    singular: "Brand",
    fields: [
      { key: "name", label: "Name", type: "text", required: true },
      { key: "country", label: "Country", type: "text" },
    ],
  },
  "car-models": {
    slug: "car-models",
    route: "car-models",
    label: "Car Models",
    singular: "Car Model",
    fields: [
      { key: "brandId", label: "Brand", type: "select", required: true, lookup: { route: "brands", valueKey: "id", labelKey: "name" } },
      { key: "name", label: "Name", type: "text", required: true },
      { key: "yearStart", label: "Year Start", type: "number" },
      { key: "yearEnd", label: "Year End", type: "number" },
    ],
  },
  categories: {
    slug: "categories",
    route: "categories",
    label: "Categories",
    singular: "Category",
    fields: [
      { key: "name", label: "Name", type: "text", required: true },
      { key: "slug", label: "Slug", type: "text", required: true },
      {
        key: "type",
        label: "Type",
        type: "select",
        required: true,
        options: [
          { value: "Car", label: "Car" },
          { value: "Part", label: "Part" },
        ],
      },
    ],
  },
  cars: {
    slug: "cars",
    route: "cars",
    label: "Cars",
    singular: "Car",
    images: true,
    fields: [
      { key: "brandId", label: "Brand", type: "select", lookup: { route: "brands", valueKey: "id", labelKey: "name" } },
      { key: "modelId", label: "Model", type: "select", lookup: { route: "car-models", valueKey: "id", labelKey: "name" } },
      { key: "year", label: "Year", type: "number", required: true },
      { key: "price", label: "Price", type: "decimal", required: true },
      { key: "mileage", label: "Mileage", type: "number" },
      { key: "color", label: "Color", type: "text" },
      { key: "categoryId", label: "Category", type: "select", lookup: { route: "categories", valueKey: "id", labelKey: "name" } },
      { key: "status", label: "Status", type: "select", required: true, options: statusOptions },
      { key: "quantity", label: "Quantity", type: "number" },
      { key: "description", label: "Description", type: "textarea" },
      { key: "specs", label: "Specs", type: "textarea" },
    ],
  },
  parts: {
    slug: "parts",
    route: "parts",
    label: "Parts",
    singular: "Part",
    images: true,
    fields: [
      { key: "name", label: "Name", type: "text", required: true },
      { key: "brand", label: "Brand", type: "text" },
      { key: "sku", label: "SKU", type: "text" },
      { key: "price", label: "Price", type: "decimal", required: true },
      { key: "quantity", label: "Quantity", type: "number" },
      { key: "status", label: "Status", type: "select", required: true, options: statusOptions },
      { key: "categoryId", label: "Category", type: "select", lookup: { route: "categories", valueKey: "id", labelKey: "name" } },
      { key: "description", label: "Description", type: "textarea" },
      { key: "specs", label: "Specs", type: "textarea" },
    ],
  },
  orders: {
    slug: "orders",
    route: "orders",
    label: "Orders",
    singular: "Order",
    canCreate: false,
    fields: [
      { key: "buyerId", label: "Buyer", type: "select", hiddenOnEdit: true, lookup: { route: "users", valueKey: "id", labelKey: "email" } },
      {
        key: "status",
        label: "Status",
        type: "select",
        required: true,
        options: [
          { value: "Pending", label: "Pending" },
          { value: "Processing", label: "Processing" },
          { value: "Shipped", label: "Shipped" },
          { value: "Delivered", label: "Delivered" },
          { value: "Completed", label: "Completed" },
          { value: "Cancelled", label: "Cancelled" },
        ],
      },
      { key: "totalAmount", label: "Total", type: "decimal", hiddenOnEdit: true },
      { key: "createdAt", label: "Created", type: "text", hiddenOnCreate: true, hiddenOnEdit: true },
    ],
  },
  "order-items": {
    slug: "order-items",
    route: "order-items",
    label: "Order Items",
    singular: "Order Item",
    canCreate: false,
    canEdit: false,
    canDelete: false,
    fields: [
      { key: "orderId", label: "Order", type: "text" },
      { key: "productId", label: "Product", type: "text" },
      { key: "quantity", label: "Quantity", type: "number" },
      { key: "unitPrice", label: "Unit Price", type: "decimal" },
    ],
  },
  reviews: {
    slug: "reviews",
    route: "reviews",
    label: "Reviews",
    singular: "Review",
    canCreate: false,
    canEdit: false,
    fields: [
      { key: "productId", label: "Product", type: "text" },
      { key: "userId", label: "User", type: "text" },
      { key: "rating", label: "Rating", type: "number" },
      { key: "comment", label: "Comment", type: "text" },
      { key: "createdAt", label: "Created", type: "text", hiddenOnCreate: true, hiddenOnEdit: true },
    ],
  },
  menu: {
    slug: "menu",
    route: "menu",
    label: "Menu",
    singular: "Menu Item",
    canEdit: true,
    fields: [
      { key: "label", label: "Label", type: "text", required: true },
      { key: "url", label: "URL", type: "text", required: true },
      { key: "icon", label: "Icon", type: "text" },
      {
        key: "isActive",
        label: "Active",
        type: "checkbox",
        hiddenOnCreate: true,
      },
      {
        key: "order",
        label: "Order",
        type: "number",
        hiddenOnCreate: true,
        hiddenOnEdit: true,
      },
    ],
  },
  "cart-items": {
    slug: "cart-items",
    route: "cart-items",
    label: "Cart Items",
    singular: "Cart Item",
    fields: [
      { key: "userId", label: "User", type: "text", required: true },
      { key: "productId", label: "Product", type: "text", required: true },
      { key: "quantity", label: "Quantity", type: "number", required: true },
    ],
  },
  favorites: {
    slug: "favorites",
    route: "favorites",
    label: "Favorites",
    singular: "Favorite",
    canCreate: false,
    canEdit: false,
    fields: [
      { key: "userId", label: "User", type: "text" },
      { key: "productId", label: "Product", type: "text" },
      { key: "createdAt", label: "Created", type: "text", hiddenOnCreate: true, hiddenOnEdit: true },
    ],
  },
};
