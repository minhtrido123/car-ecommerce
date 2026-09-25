"use client";
import { useEffect, useState, useCallback } from "react";
import Link from "next/link";
import api from "../lib/api";
import Pagination from "../components/Pagination";
import RangeFilter from "../components/RangeFilter";
import { Car, CarFiltersMeta, Filters, PagedCars } from "./types";
import { useSearchParams } from "next/navigation";
import { useRouter } from "next/navigation";

const PAGE_SIZE = 20;
const SORT_COLUMNS = ["price", "year", "mileage", "createdAt"] as const;
type SortColumn = (typeof SORT_COLUMNS)[number];

const DEFAULT_FILTERS: Filters = {
  minPrice: null,
  maxPrice: null,
  minMileage: null,
  maxMileage: null,
  minYear: null,
  maxYear: null,
  color: "",
  status: "",
  brandIds: [],
};

export default function CarsPage() {
  const [cars, setCars] = useState<Car[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [meta, setMeta] = useState<CarFiltersMeta | null>(null);
  const [showFilters, setShowFilters] = useState(true);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [sortBy, setSortBy] = useState<SortColumn>("createdAt");
  const [sortDir, setSortDir] = useState<"asc" | "desc">("desc");
  const [pageNumber, setPageNumber] = useState(1);
  const [filters, setFilters] = useState<Filters>(DEFAULT_FILTERS);
  const [debouncedFilters, setDebouncedFilters] = useState<Filters>(DEFAULT_FILTERS);
  const searchParams = useSearchParams();
  const router = useRouter();

  useEffect(() => {
    api
      .get("/cars/filters", { showLoading: false })
      .then(({ data }) => setMeta(data))
      .catch(() => setMeta(null));
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => {
      setLoading(true);
      setDebouncedSearch(search);
      setPageNumber(1);
    }, 400);
    return () => clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    const timer = setTimeout(() => {
      setLoading(true);
      setDebouncedFilters(filters);
      setPageNumber(1);
    }, 500);
    return () => clearTimeout(timer);
  }, [filters]);

  const fetchCars = useCallback(
    async (
      page: number,
      q: string,
      by: SortColumn,
      dir: "asc" | "desc",
      f: Filters
    ): Promise<PagedCars> => {
      const params = new URLSearchParams({
        pageNumber: String(page),
        pageSize: String(PAGE_SIZE),
        sortBy: by,
        sortDir: dir,
        includes: "ProductImages",
      });
      if (q)
        params.set("search", q);
      if (f.minPrice != null) params.set("minPrice", String(f.minPrice));
      if (f.maxPrice != null) params.set("maxPrice", String(f.maxPrice));
      if (f.minMileage != null) params.set("minMileage", String(f.minMileage));
      if (f.maxMileage != null) params.set("maxMileage", String(f.maxMileage));
      if (f.minYear != null) params.set("minYear", String(f.minYear));
      if (f.maxYear != null) params.set("maxYear", String(f.maxYear));
      if (f.color) params.set("color", f.color);
      if (f.status) params.set("status", f.status);
      if (f.brandIds.length > 0) params.set("brandIds", f.brandIds.join(","));

      const filteredParams = new URLSearchParams(
        [...params.entries()].filter(([key]) => key !== "includes")
      );

      router.push(`/cars?${filteredParams.toString()}`);

      const { data } = await api.get(`/cars?${params.toString()}`, { showLoading: false });
      return {
        items: data.items ?? data,
        totalCount: data.totalCount ?? 0,
        pageNumber: data.pageNumber ?? page,
        pageSize: data.pageSize ?? PAGE_SIZE,
      };
    },
    []
  );

  useEffect(() => {
    let cancelled = false;
    fetchCars(pageNumber, debouncedSearch, sortBy, sortDir, debouncedFilters)
      .then((data) => {
        if (cancelled) return;
        setCars(data.items);
        setTotalCount(data.totalCount);
      })
      .catch(() => {
        if (cancelled) return;
        setCars([]);
        setTotalCount(0);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [fetchCars, pageNumber, debouncedSearch, sortBy, sortDir, debouncedFilters]);

  const handleSort = (column: SortColumn) => {
    setLoading(true);
    if (column === sortBy) {
      setSortDir((dir) => (dir === "asc" ? "desc" : "asc"));
    } else {
      setSortBy(column);
      setSortDir(column === "createdAt" ? "desc" : "asc");
    }
    setPageNumber(1);
  };

  const handlePageChange = (page: number) => {
    setLoading(true);
    setPageNumber(page);
  };

  const handleResetFilters = () => {
    setFilters(DEFAULT_FILTERS);
    setDebouncedFilters(DEFAULT_FILTERS);
    setPageNumber(1);
  };

  const from = totalCount === 0 ? 0 : (pageNumber - 1) * PAGE_SIZE + 1;
  const to = Math.min(pageNumber * PAGE_SIZE, totalCount);

  return (
    <div className="px-5!">
      <p className="text-center h3 text-light">Sort by</p>
      <div className="d-flex flex-justify-center flex-wrap m-2">
        {SORT_COLUMNS.map((column) => (
          <button
            key={column}
            className={`button m-1 ${sortBy === column ? "info" : ""}`}
            onClick={() => handleSort(column)}
          >
            {column.charAt(0).toUpperCase() + column.slice(1)}
            <span className={`mif-arrow-${sortBy === column && sortDir === "desc" ? "down" : "up"}`}></span>
          </button>
        ))}
      </div>

      <div className="d-flex flex-justify-center m-2">
        <button className="button m-1" onClick={() => setShowFilters((v) => !v)}>
          Filters
          <span className={`mif-chevron-${showFilters ? "down" : "up"}`}></span>
        </button>
        <button className="button secondary m-1" onClick={handleResetFilters}>
          Reset filters
        </button>
      </div>

      {showFilters && meta && (
        <div className="border bd-default p-2 m-2">
          <div className="row">
            <div className="cell-md-4 d-flex">
              <RangeFilter
                label="Price"
                min={meta.minPrice}
                max={meta.maxPrice}
                step={500}
                lo={filters.minPrice}
                hi={filters.maxPrice}
                format={(n) => `$${n.toLocaleString()}`}
                onChange={(lo, hi) => setFilters((f) => ({ ...f, minPrice: lo, maxPrice: hi }))}
              />
            </div>
            <div className="cell-md-4 d-flex">
              <RangeFilter
                label="Mileage"
                min={meta.minMileage}
                max={meta.maxMileage}
                step={1000}
                lo={filters.minMileage}
                hi={filters.maxMileage}
                format={(n) => `${n.toLocaleString()} km`}
                onChange={(lo, hi) => setFilters((f) => ({ ...f, minMileage: lo, maxMileage: hi }))}
              />
            </div>
            <div className="cell-md-4 d-flex">
              <RangeFilter
                label="Year"
                min={meta.minYear}
                max={meta.maxYear}
                step={1}
                lo={filters.minYear}
                hi={filters.maxYear}
                format={(n) => String(n)}
                onChange={(lo, hi) => setFilters((f) => ({ ...f, minYear: lo, maxYear: hi }))}
              />
            </div>
          </div>
          <div className="d-flex flex-wrap flex-justify-center m-2">
            <select
              className="select m-1"
              value={filters.color}
              onChange={(e) => setFilters((f) => ({ ...f, color: e.target.value }))}
            >
              <option value="">All colors</option>
              {meta.colors.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </select>
            <select
              className="select m-1"
              value={filters.status}
              onChange={(e) => setFilters((f) => ({ ...f, status: e.target.value }))}
            >
              <option value="">All statuses</option>
              {meta.statuses.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </div>
          <div className="d-flex flex-wrap flex-justify-center m-2">
            {meta.brands.map((b) => (
              <label key={b.id} className="checkbox m-1">
                <input
                  type="checkbox"
                  checked={filters.brandIds.includes(b.id)}
                  onChange={() =>
                    setFilters((f) => ({
                      ...f,
                      brandIds: f.brandIds.includes(b.id)
                        ? f.brandIds.filter((id) => id !== b.id)
                        : [...f.brandIds, b.id],
                    }))
                  }
                />
                <span className="check"></span>
                {b.name}
              </label>
            ))}
          </div>
        </div>
      )}

      <div className="list-search-block d-flex flex-justify-center">
        <div className="input">
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <div className="button-group">
            <button
              className="button input-clear-button"
              type="button"
              onClick={() => setSearch("")}
            >
              <span className="default-icon-cross"></span>
            </button>
          </div>
          <div className="prepend">Search:</div>
        </div>
      </div>

      <ul className="unstyled-list row mt-4">
        {cars.map((car) => {
          const imageUrl =
            car.productImages && car.productImages.length > 0
              ? car.productImages[0].url
              : "/ford.png";
          return (
            <li key={car.id} className="cell-sm-6 cell-md-3">
              <Link href={`/cars/${car.id}`} className="card image-header m-0 p-0 w-full">
                <div
                  className="card-header fg-white"
                  style={{
                    backgroundImage: `url("${imageUrl}")`,
                  }}
                ></div>
                <div className="card-content p-2">
                  {(car.brand || car.model) && (
                    <p className="text-lg font-semibold text-gray-900">
                      {[car.brand?.name, car.model?.name].filter(Boolean).join(" ")}
                    </p>
                  )}
                  <p className="fg-gray">
                    {car.year} — {car.color ?? "Unknown"}
                  </p>
                  <p className="text-2xl font-bold text-gray-900 mb-2">
                    ${car.price.toLocaleString()}
                  </p>
                  {car.mileage != null && (
                    <p className="text-sm text-gray-600 mb-1">
                      Mileage: {car.mileage.toLocaleString()} km
                    </p>
                  )}
                  <p className="text-sm text-gray-600 mb-1">
                    Status:{" "}
                    <span
                      className={`inline-block px-2 py-0.5 rounded text-xs font-medium ${car.status === "Active"
                        ? "bg-green-100 text-green-800"
                        : car.status === "Sold"
                          ? "bg-red-100 text-red-800"
                          : "bg-yellow-100 text-yellow-800"
                        }`}
                    >
                      {car.status}
                    </span>
                  </p>
                  {car.description && (
                    <p className="text-sm text-gray-500 mt-2 line-clamp-2">
                      {car.description}
                    </p>
                  )}
                </div>
              </Link>
            </li>
          );
        })}
        {cars.length === 0 && loading && (
          <>
            {Array.from({ length: 3 }).map((_, id) => (
              <li key={id} className="cell-sm-6 cell-md-4">
                <div className="card image-header animate-pulse">
                  <div
                    className="card-header fg-white"
                    style={{ background: "gray" }}
                  ></div>
                  <div className="card-content p-2">
                    <p className="h-2 rounded bg-gray-200"></p>
                    <p className="h-2 rounded bg-gray-200"></p>
                    <p className="h-2 rounded bg-gray-200"></p>
                    <p className="h-2 rounded bg-gray-200"></p>
                    <p className="h-2 rounded bg-gray-200"></p>
                  </div>
                  <div className="card-footer">
                    <button className="button secondary">View Details</button>
                  </div>
                </div>
              </li>
            ))}
          </>
        )}
        {cars.length === 0 && !loading && (
          <p className="text-center h4 text-light mt-4">No cars found</p>
        )}
      </ul>

      {totalCount > 0 && (
        <div className="d-flex flex-justify-center flex-wrap m-2">
          <div className="list-info text-center m-2">
            Showing {from} to {to} of {totalCount} cars
          </div>
          <div className="list-pagination">
            <Pagination
              currentPage={pageNumber}
              totalCount={totalCount}
              pageSize={PAGE_SIZE}
              onPageChange={handlePageChange}
            />
          </div>
        </div>
      )}
    </div>
  );
}
