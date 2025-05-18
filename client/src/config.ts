interface Config {
    httpUrl: string;
    wsUrl: string;
}

const config: Config = {
    httpUrl: import.meta.env.VITE_HTTP_URL || "__VITE_HTTP_URL__",
    wsUrl: import.meta.env.VITE_WS_URL || "__VITE_WS_URL__"
};

export default config;