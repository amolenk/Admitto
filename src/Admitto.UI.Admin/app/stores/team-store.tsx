import { create } from "zustand";

type TeamStore = {
    selectedTeamId: string | null;
    setSelectedTeamId: (teamId: string | null) => void;
};

export const useTeamStore = create<TeamStore>((set) => ({
    selectedTeamId: null,
    setSelectedTeamId: (teamId: string | null) =>
    {
        set({ selectedTeamId: teamId });
    },
}));
