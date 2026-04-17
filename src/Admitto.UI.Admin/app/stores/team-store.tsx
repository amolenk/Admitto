import { create } from "zustand";

type TeamStore = {
    selectedTeamSlug: string | null;
    setSelectedTeamSlug: (teamSlug: string | null) => void;
};

export const useTeamStore = create<TeamStore>((set) => ({
    selectedTeamSlug: null,
    setSelectedTeamSlug: (teamSlug: string | null) =>
    {
        set({ selectedTeamSlug: teamSlug });
    },
}));
