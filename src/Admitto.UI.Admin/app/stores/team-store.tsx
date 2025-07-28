import { create } from "zustand";
import { TeamDto } from "@/api-client";

type TeamStore = {
    teams: TeamDto[];
    selectedTeam: TeamDto | null;
    isLoading: boolean,
    hasLoaded: boolean,
    setSelectedTeamId: (teamId: string) => void;
    fetchTeams: (selectTeamId?: string) => Promise<void>;
};

export const useTeamStore = create<TeamStore>((set) => ({
    teams: [],
    selectedTeam: null,
    isLoading: false,
    hasLoaded: false,
    setSelectedTeamId: (teamId: string) =>
    {
        const team = useTeamStore.getState().teams.find((t) => t.id === teamId);
        set({ selectedTeam: team });
    },
    fetchTeams: async (selectTeamId?: string) =>
    {

        set({ isLoading: true });

        try
        {
            const response = await fetch("/api/teams", { method: "GET" });

            if (!response.ok)
            {
                throw new Error("Failed to fetch teams");
            }

            const teams = (await response.json()).teams;

            set({ teams });

            if (teams.length > 0)
            {
                if (selectTeamId)
                {
                    const team = teams.find((t: TeamDto) => t.id === selectTeamId);
                    set({ selectedTeam: team });
                }
                else
                {
                    set({ selectedTeam: teams[0] });
                }
            }

            set({ isLoading: false, hasLoaded: true });

        }
        catch (error)
        {
            console.error("Failed to add team:", error);
        }
    }
}));
