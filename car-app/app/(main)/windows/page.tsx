import Tiles from "../components/Tiles";

export default function Home() {
  return (
    <div style={{
      backgroundImage:"url(/DesktopBackground/8_classicsportscars_mazdamiata.jpg)",
      backgroundSize: "cover",
      backgroundPosition: "center",
      backgroundRepeat: "no-repeat",
      minHeight: "100vh",
      }}>
        <Tiles/>
    </div>
  );
}

