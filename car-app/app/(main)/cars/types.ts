export type CarImage = {
  id: string;
  productId: string;
  url: string;
  isPrimary: boolean;
  createdAt: string;
};

export type CarBrand = {
  id: string;
  name: string;
  country: string | null;
};

export type CarModel = {
  id: string;
  brandId: string;
  name: string;
  yearStart: number | null;
  yearEnd: number | null;
};

export type Car = {
  id: string;
  brandId: string | null;
  modelId: string | null;
  categoryId: string | null;
  sellerId: string | null;
  year: number;
  price: number;
  mileage: number | null;
  color: string | null;
  description: string | null;
  status: string;
  createdAt: string;
  productImages: CarImage[];
  brand: CarBrand | null;
  model: CarModel | null;
};

export type CarFiltersMeta = {
  brands: { id: string; name: string }[];
  colors: string[];
  statuses: string[];
  minPrice: number;
  maxPrice: number;
  minMileage: number;
  maxMileage: number;
  minYear: number;
  maxYear: number;
};

export type Filters = {
  minPrice: number | null;
  maxPrice: number | null;
  minMileage: number | null;
  maxMileage: number | null;
  minYear: number | null;
  maxYear: number | null;
  color: string;
  status: string;
  brandIds: string[];
};

export type PagedCars = {
  items: Car[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
};


// export { Car, CarBrand, CarFiltersMeta, CarModel, CarImage, Filters, PagedCars };
