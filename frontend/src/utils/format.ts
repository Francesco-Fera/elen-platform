import { format, formatDistanceToNow, parseISO } from "date-fns";

export const formatUtils = {
  // Date formatting
  formatDate: (date: string | Date, formatStr = "MMM d, yyyy") => {
    const dateObj = typeof date === "string" ? parseISO(date) : date;
    return format(dateObj, formatStr);
  },

  formatDateTime: (date: string | Date) => {
    const dateObj = typeof date === "string" ? parseISO(date) : date;
    return format(dateObj, "MMM d, yyyy 'at' h:mm a");
  },

  formatTimeAgo: (date: string | Date) => {
    const dateObj = typeof date === "string" ? parseISO(date) : date;
    return formatDistanceToNow(dateObj, { addSuffix: true });
  },

  // Text formatting
  capitalizeFirst: (str: string) => {
    return str.charAt(0).toUpperCase() + str.slice(1).toLowerCase();
  },

  truncateText: (text: string, maxLength: number) => {
    return text.length > maxLength ? `${text.slice(0, maxLength)}...` : text;
  },

  // Number formatting
  formatNumber: (num: number, decimals = 0) => {
    return new Intl.NumberFormat("en-US", {
      minimumFractionDigits: decimals,
      maximumFractionDigits: decimals,
    }).format(num);
  },

  // File size formatting
  formatFileSize: (bytes: number) => {
    const sizes = ["Bytes", "KB", "MB", "GB"];
    if (bytes === 0) return "0 Bytes";
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return `${Math.round((bytes / Math.pow(1024, i)) * 100) / 100} ${sizes[i]}`;
  },

  // Organization role formatting
  formatRole: (role: string) => {
    const roleMap: Record<string, string> = {
      Owner: "Owner",
      Admin: "Administrator",
      Member: "Member",
      Viewer: "Viewer",
    };
    return roleMap[role] || role;
  },

  // Subscription plan formatting
  formatSubscriptionPlan: (plan: string) => {
    const planMap: Record<string, string> = {
      Free: "Free",
      Pro: "Pro",
      Enterprise: "Enterprise",
    };
    return planMap[plan] || plan;
  },
};
