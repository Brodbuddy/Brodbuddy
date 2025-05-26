import {atom} from "jotai/index";
import {DiagnosticsResponse} from "@/api/websocket-client.ts";

export const diagnosticsAtom = atom<DiagnosticsResponse[]>([]);