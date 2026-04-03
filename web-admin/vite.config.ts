import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Your API endpoints
      "/api": {
        target: "https://localhost:7234",
        changeOrigin: true,
        secure: false,
      },
      // Your SignalR hub
      "/hubs": {
        target: "https://localhost:7234",
        changeOrigin: true,
        secure: false,
        ws: true,
      },
    },
  },
});
