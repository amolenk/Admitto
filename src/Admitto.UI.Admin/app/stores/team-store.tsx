import { create } from "zustand";

type TeamStore = {
    selectedTeamSlug: string | null;
    setSelectedTeamSlug: (teamSlug: string) => void;
};

export const useTeamStore = create<TeamStore>((set) => ({
    selectedTeamSlug: null,
    setSelectedTeamSlug: (teamSlug: string) =>
    {
        set({ selectedTeamSlug: teamSlug });
    },
}));
