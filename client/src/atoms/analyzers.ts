import { atom } from "jotai";
import { Analyzer } from "../models/analyzer";

export const analyzersAtom = atom<Analyzer[]>([]);