"use client";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import api from "../../lib/api";
import { isAxiosError } from "axios";
import { useAuthStore } from "../../stores/auth-store";

type CarImage = { id: string; productId: string; url: string; isPrimary: boolean; createdAt: string };
type Seller = { id: string; name: string; email: string };
type CarDetail = {
  id: string;
  modelId: string | null;
  year: number;
  price: number;
  mileage: number | null;
  color: string | null;
  description: string | null;
  status: string;
  createdAt: string;
  brandName: string | null;
  modelName: string | null;
  categoryName: string | null;
  seller: Seller | null;
  images: CarImage[];
};

type SimilarCar = {
  id: string;
  brandName: string | null;
  modelName: string | null;
  year: number;
  price: number;
  mileage: number | null;
  color: string | null;
  primaryImageUrl: string | null;
};

export default function CarDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const accessToken = useAuthStore((state) => state.accessToken);

  const [car, setCar] = useState<CarDetail | null>(null);
  const [similar, setSimilar] = useState<SimilarCar[]>([]);
  const [selectedImage, setSelectedImage] = useState(0);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    async function load() {
      try {
        const { data } = await api.get(`/cars/${id}`, { showLoading: false });
        setCar(data);
        setSelectedImage(0);
        if (data.modelId) {
          const sim = await api.get(`/cars/by-model/${data.modelId}?limit=4&exclude=${id}`, { showLoading: false });
          setSimilar(sim.data);
        }
      } catch (err: unknown) {
        if (isAxiosError(err) && err.response?.status === 404) setNotFound(true);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [id]);

  async function addFavorite() {
    if (!accessToken) {
      router.push("/login");
      return;
    }
    try {
      await api.post("/me/favorites", { productId: id }, { showLoading: false });
      alert("Added to favorites");
    } catch (err: unknown) {
      if (isAxiosError(err) && err.response?.status === 409) alert("Already in favorites");
    }
  }

  async function addToCart() {
    if (!accessToken) {
      router.push("/login");
      return;
    }
    try {
      await api.post("/me/cart-items", { productId: id, quantity: 1 }, { showLoading: false });
      alert("Added to cart");
    } catch (err: unknown) {
      if (isAxiosError(err) && err.response?.status === 409) alert("Already in cart");
    }
  }

  if (loading) {
    return (
      <div className="px-5!">
        <h1 className="h1 text-center my-6">Loading…</h1>
        <div className="card image-header animate-pulse">
          <div className="card-header" style={{ background: "gray", height: "300px" }} />
        </div>
      </div>
    );
  }

  if (notFound || !car) {
    return (
      <div className="px-5! text-center my-10">
        <h1 className="h1">Car not found</h1>
        <p className="h3">The listing you are looking for does not exist.</p>
        <Link href="/cars" className="button">Back to Cars</Link>
      </div>
    );
  }

  const mainImage = car.images.length > 0 ? car.images[selectedImage].url : "/ford.png";

  return (
    <div className="px-5! max-w-[1100px]! mx-auto!">
      <div className="my-4">
        <Link href="/cars" className="button secondary">← Back to Cars</Link>
      </div>

      <h1 className="h1 my-4">
        {car.year} {car.brandName ?? ""} {car.modelName ?? ""}
      </h1>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div>
          <div className="card">
            <div className="card-header p-0">
              <img
                src={mainImage}
                alt={`${car.brandName ?? ""} ${car.modelName ?? ""}`}
                className="w-full! h-[320px]! object-cover!"
              />
            </div>
            {car.images.length > 1 && (
              <div className="card-content d-flex flex-wrap p-2">
                {car.images.map((img, i) => (
                  <img
                    key={img.id}
                    src={img.url}
                    alt=""
                    onClick={() => setSelectedImage(i)}
                    className={`w-[80px]! h-[60px]! object-cover! m-1! border! cursor-pointer ${
                      i === selectedImage ? "border-blue-600" : "border-gray-300"
                    }`}
                  />
                ))}
              </div>
            )}
          </div>

          <div className="card mt-4">
            <div className="card-content p-4">
              <h2 className="h3">Specifications</h2>
              <table className="table table-striped">
                <tbody>
                  <tr><td className="w-[40%]!">Brand</td><td>{car.brandName ?? "—"}</td></tr>
                  <tr><td>Model</td><td>{car.modelName ?? "—"}</td></tr>
                  <tr><td>Category</td><td>{car.categoryName ?? "—"}</td></tr>
                  <tr><td>Year</td><td>{car.year}</td></tr>
                  <tr><td>Price</td><td className="text-2xl! font-bold!">${car.price.toLocaleString()}</td></tr>
                  <tr><td>Mileage</td><td>{car.mileage != null ? `${car.mileage.toLocaleString()} km` : "—"}</td></tr>
                  <tr><td>Color</td><td>{car.color ?? "—"}</td></tr>
                  <tr><td>Status</td><td>{car.status}</td></tr>
                </tbody>
              </table>

              {car.description && (
                <div className="mt-4">
                  <h3 className="h4">Description</h3>
                  <p className="text-gray-600">{car.description}</p>
                </div>
              )}

              <div className="d-flex flex-wrap mt-4">
                <button className="button success m-1" onClick={addFavorite}>Add to Favorites</button>
                <button className="button info m-1" onClick={addToCart}>Add to Cart</button>
              </div>
            </div>
          </div>
        </div>

        <div>
          {car.seller && (
            <div className="card">
              <div className="card-header">Seller</div>
              <div className="card-content p-4">
                <p className="text-lg! font-semibold">{car.seller.name}</p>
                <p className="text-gray-600">{car.seller.email}</p>
              </div>
            </div>
          )}

          {similar.length > 0 && (
            <div className="card mt-4">
              <div className="card-header">Similar Cars</div>
              <div className="card-content p-4">
                {similar.map((s) => (
                  <Link key={s.id} href={`/cars/${s.id}`} className="d-flex items-center! p-2 border! mb-2! no-hover">
                    <img
                      src={s.primaryImageUrl ?? "/ford.png"}
                      alt=""
                      className="w-[100px]! h-[70px]! object-cover!"
                    />
                    <div className="ml-3">
                      <p className="font-semibold">{s.year} {s.brandName ?? ""} {s.modelName ?? ""}</p>
                      <p className="text-gray-600">${s.price.toLocaleString()} · {s.mileage?.toLocaleString()} km</p>
                    </div>
                  </Link>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
