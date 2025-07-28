import NextAuth from "next-auth"
import Keycloak from "next-auth/providers/keycloak"
 
export const { handlers, auth, signIn, signOut } = NextAuth({
  providers: [Keycloak]
  });
