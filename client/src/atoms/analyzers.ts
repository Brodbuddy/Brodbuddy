import { atom } from "jotai";
import { AnalyzerListResponse } from "../api/Api";

export const analyzersAtom = atom<AnalyzerListResponse[]>([]);