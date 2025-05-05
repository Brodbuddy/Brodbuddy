import path from "node:path";
import { generateApi } from "swagger-typescript-api";

generateApi({
    name: "http-client.ts",
    output: path.resolve(process.cwd(), "./src/api"),
    url: "http://localhost:5001/swagger/v1/swagger.json",
    httpClientType: "axios",
    moduleNameIndex: 1, // Lav så man har api.kartoffel, api.kakao, api.pepsi, etc.
    hooks: {
        onFormatRouteName: (routeInfo, templateRouteName) => {
            const { moduleName } = routeInfo;
            let name = templateRouteName;

            // Fjern modul navn prefix 'pepsiDrinkPepsi' til 'DrinkPepsi'
            if (moduleName && name) {
                const moduleNameLower = moduleName.toLowerCase();
                if (name.toLowerCase().startsWith(moduleNameLower)) {
                    name = name.substring(moduleName.length);
                }
            }

            // Lav om til camelCase 'DrinkPepsi' til 'drinkPepsi'
            if (name) {
                name = name.charAt(0).toLowerCase() + name.slice(1);
            } else {
                console.warn("Method name is undefined for route:", routeInfo.route);
                name = "unnamedMethod";
            }

            return name;
        },
    },
}).catch((e) => console.error(e));