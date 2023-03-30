import {ConfigApi, Configuration, RoutingApi, type RoutingConfig} from "../routing-api-client";
import {error} from "@sveltejs/kit";

export async function load(): Promise<RoutingConfig> {
  const api = new ConfigApi(new Configuration({
    basePath: "http://localhost:5276"
  }));

  return await api.configGet()
    .catch((e) => {
      console.error(e)
      throw error(500, "Something went wrong.");
    })
}
