import { Menu, X, LogOut, Home, Shield } from "lucide-react"
import { Sidebar, SidebarContent, SidebarFooter, useSidebar } from "@/components/ui/sidebar"
import { cn } from "@/lib/utils"
import { Link, useLocation } from "react-router-dom"
import { AppRoutes } from "@/helpers"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/hooks/useAuth"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { useAtomValue } from "jotai"
import { userInfoAtom } from "@/atoms/auth"

export function AppSidebar() {
    const { open, setOpen } = useSidebar()
    const auth = useAuth()
    const location = useLocation()
    const userInfo = useAtomValue(userInfoAtom)

    // Get user initials for avatar
    const getUserInitials = () => {
        if (!auth.user?.email) return "?";
        const email = auth.user.email;
        const username = email.split('@')[0];
        return username.substring(0, 2).toUpperCase();
    };

    return (
        <>
            {!open && (
                <button
                    onClick={() => setOpen(true)}
                    className="fixed top-4 left-4 p-3 bg-accent-foreground text-primary-foreground rounded-md z-30 shadow-md transition-transform hover:scale-105"
                    aria-label="Open sidebar"
                >
                    <Menu className="h-6 w-6" />
                </button>
            )}

            {/* Backdrop overlay - only show when sidebar is open */}
            {open && (
                <div
                    className="fixed inset-0 bg-black/50 z-30"
                    onClick={() => setOpen(false)}
                />
            )}

            {/* Main sidebar */}
            <Sidebar className={cn(
                "fixed left-0 top-0 h-full bg-secondary text-accent-foreground transition-all duration-300 ease-in-out z-40 border-r border-border-brown shadow-lg",
                open ? "w-[280px] translate-x-0" : "w-[280px] -translate-x-full"
            )}>
                <div className="flex justify-between items-center p-4 border-b border-border-brown bg-accent-foreground/5">
                    <h2 className="text-lg font-bold text-accent-foreground">BrodBuddy</h2>
                    <button
                        onClick={() => setOpen(false)}
                        className="text-accent-foreground p-2 rounded-full hover:bg-accent-foreground/10 transition-colors"
                        aria-label="Close sidebar"
                    >
                        <X className="h-5 w-5" />
                    </button>
                </div>
                <SidebarContent className="p-4 overflow-y-auto">
                    <nav>
                        <ul className="space-y-1">
                            <li>
                                <Link
                                    to={AppRoutes.home}
                                    className={cn(
                                        "flex items-center py-2.5 px-4 rounded-md transition-colors",
                                            location.pathname === AppRoutes.home
                                            ? "bg-accent-foreground text-primary font-medium"
                                            : "hover:bg-accent hover:text-accent-foreground"
                                    )}
                                    onClick={() => setOpen(false)}
                                >
                                    <Home className="h-5 w-5 mr-3" />
                                    <span>Dashboard</span>
                                </Link>
                            </li>
                            {userInfo?.isAdmin && (
                                <li>
                                    <Link
                                        to={AppRoutes.admin}
                                        className={cn(
                                            "flex items-center py-2.5 px-4 rounded-md transition-colors",
                                            location.pathname === AppRoutes.admin
                                                ? "bg-accent-foreground text-primary font-medium"
                                                : "hover:bg-accent hover:text-accent-foreground"
                                        )}
                                        onClick={() => setOpen(false)}
                                    >
                                        <Shield className="h-5 w-5 mr-3" />
                                        <span>Admin</span>
                                    </Link>
                                </li>
                            )}
                        </ul>
                    </nav>
                </SidebarContent>
                <SidebarFooter className="border-t border-border-brown p-4 bg-accent-foreground/5">
                    <div className="flex items-center justify-between gap-3 w-full">
                        <div className="flex items-center gap-3">
                            <Avatar className="h-9 w-9 bg-accent-foreground text-primary">
                                <AvatarFallback>{getUserInitials()}</AvatarFallback>
                            </Avatar>
                            <div className="flex-1 truncate">
                                <p className="text-sm font-medium">{auth.user?.email ? auth.user.email.split('@')[0] : "Guest"}</p>
                                <p className="text-xs text-accent-foreground/70">{auth.user?.email}</p>
                            </div>
                        </div>
                        <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 rounded-full text-red-500 hover:bg-red-100"
                            onClick={() => auth.logout()}
                        >
                            <LogOut className="h-4 w-4" />
                            <span className="sr-only">Log out</span>
                        </Button>
                    </div>
                </SidebarFooter>
            </Sidebar>
        </>
    )
}