import { create } from "zustand";
import { TeamDto } from "@/lib/admitto-api/generated/types.gen";

type TeamStore = {
    teams: TeamDto[];
    selectedTeam: TeamDto | null;
    isLoading: boolean,
    hasLoaded: boolean,
    setSelectedTeamSlug: (teamSlug: string) => void;
    fetchTeams: (selectTeamSlug?: string) => Promise<void>;
};

export const useTeamStore = create<TeamStore>((set) => ({
    teams: [],
    selectedTeam: null,
    isLoading: false,
    hasLoaded: false,
    setSelectedTeamSlug: (teamSlug: string) =>
    {
        const team = useTeamStore.getState().teams.find((t) => t.slug === teamSlug);
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
                    const team = teams.find((t: TeamDto) => t.slug === selectTeamId);
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
