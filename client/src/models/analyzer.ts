export interface Analyzer {
  id: string;
  name: string;
  nickname?: string | null;
  lastSeen?: string | null;
  isOwner: boolean;
  activatedAt?: string;
}