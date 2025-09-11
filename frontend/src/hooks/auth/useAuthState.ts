import { create } from "zustand";
import { subscribeWithSelector } from "zustand/middleware";
import type {
  User,
  Organization,
  AuthState,
  AuthErrorDetails,
  LoadingStates,
} from "../../types";

interface AuthStateStore {
  // State
  authState: AuthState;
  user: User | null;
  organization: Organization | null;
  error: AuthErrorDetails | null;
  loading: LoadingStates;
  isInitialized: boolean;

  // Actions
  setAuthState: (state: AuthState) => void;
  setUser: (user: User | null) => void;
  setOrganization: (organization: Organization | null) => void;
  setError: (error: AuthErrorDetails | null) => void;
  setLoading: (key: keyof LoadingStates, value: boolean) => void;
  setInitialized: (initialized: boolean) => void;
  reset: () => void;
}

const initialState = {
  authState: "idle" as AuthState,
  user: null,
  organization: null,
  error: null,
  loading: {
    login: false,
    register: false,
    logout: false,
    refresh: false,
    profile: false,
  },
  isInitialized: false,
};

export const useAuthState = create<AuthStateStore>()(
  subscribeWithSelector((set, get) => ({
    ...initialState,

    setAuthState: (authState) => set({ authState }),

    setUser: (user) => set({ user }),

    setOrganization: (organization) => set({ organization }),

    setError: (error) => set({ error }),

    setLoading: (key, value) =>
      set((state) => ({
        loading: { ...state.loading, [key]: value },
      })),

    setInitialized: (isInitialized) => set({ isInitialized }),

    reset: () => set(initialState),
  }))
);

// Computed selectors
export const useIsAuthenticated = () =>
  useAuthState((state) => state.authState === "authenticated");

export const useIsLoading = () =>
  useAuthState((state) => Object.values(state.loading).some(Boolean));

export const useAuthError = () => useAuthState((state) => state.error);

export const useCurrentUser = () => useAuthState((state) => state.user);

export const useCurrentOrganization = () =>
  useAuthState((state) => state.organization);
