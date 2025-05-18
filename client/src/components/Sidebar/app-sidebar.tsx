import { Menu, Settings, X, LogOut, Home, Layers, Users, AlertCircle } from "lucide-react"
import { Sidebar, SidebarContent, SidebarFooter, useSidebar } from "@/components/ui/sidebar"
import { cn } from "@/lib/utils"
import { Link, useLocation } from "react-router-dom"
import { AppRoutes } from "@/helpers"
import { Button } from "@/components/ui/button"
import { useState } from "react"
import { useAuth } from "@/hooks/useAuth"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"

export function AppSidebar() {
    const { open, setOpen } = useSidebar()
    const [showSettings, setShowSettings] = useState(false)
    const auth = useAuth()
    const location = useLocation()

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
                                    to={AppRoutes.homeDashboard}
                                    className={cn(
                                        "flex items-center py-2.5 px-4 rounded-md transition-colors",
                                        location.pathname === AppRoutes.homeDashboard
                                            ? "bg-accent-foreground text-primary font-medium"
                                            : "hover:bg-accent hover:text-accent-foreground"
                                    )}
                                    onClick={() => setOpen(false)}
                                >
                                    <Home className="h-5 w-5 mr-3" />
                                    <span>Dashboard</span>
                                </Link>
                            </li>
                            <li>
                                <Link
                                    to="/recipes"
                                    className={cn(
                                        "flex items-center py-2.5 px-4 rounded-md transition-colors",
                                        location.pathname.includes('/recipes')
                                            ? "bg-accent-foreground text-primary font-medium"
                                            : "hover:bg-accent hover:text-accent-foreground"
                                    )}
                                    onClick={() => setOpen(false)}
                                >
                                    <Layers className="h-5 w-5 mr-3" />
                                    <span>Recipes</span>
                                </Link>
                            </li>
                            <li>
                                <Link
                                    to="/community"
                                    className={cn(
                                        "flex items-center py-2.5 px-4 rounded-md transition-colors",
                                        location.pathname.includes('/community')
                                            ? "bg-accent-foreground text-primary font-medium"
                                            : "hover:bg-accent hover:text-accent-foreground"
                                    )}
                                    onClick={() => setOpen(false)}
                                >
                                    <Users className="h-5 w-5 mr-3" />
                                    <span>Community</span>
                                </Link>
                            </li>
                            <li>
                                <Link
                                    to="/help"
                                    className={cn(
                                        "flex items-center py-2.5 px-4 rounded-md transition-colors",
                                        location.pathname.includes('/help')
                                            ? "bg-accent-foreground text-primary font-medium"
                                            : "hover:bg-accent hover:text-accent-foreground"
                                    )}
                                    onClick={() => setOpen(false)}
                                >
                                    <AlertCircle className="h-5 w-5 mr-3" />
                                    <span>Help</span>
                                </Link>
                            </li>
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
                        <div className="flex items-center gap-1">
                            <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8 rounded-full hover:bg-accent-foreground/10"
                                onClick={() => setShowSettings(true)}
                            >
                                <Settings className="h-4 w-4" />
                                <span className="sr-only">Settings</span>
                            </Button>
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
                    </div>
                </SidebarFooter>
            </Sidebar>

            {/* Settings Sheet */}
            {showSettings && (
                <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex justify-end">
                    <div className="bg-bg-cream w-full max-w-md h-full p-4 sm:p-6 overflow-y-auto">
                        <div className="flex justify-between items-center mb-6">
                            <h2 className="text-xl font-bold text-accent-foreground">Settings</h2>
                            <Button
                                variant="ghost"
                                size="icon"
                                className="h-10 w-10"
                                onClick={() => setShowSettings(false)}
                            >
                                <X className="h-5 w-5" />
                                <span className="sr-only">Close</span>
                            </Button>
                        </div>

                        <div className="space-y-6">
                            {/* Settings content goes here */}
                            <div className="space-y-2">
                                <h3 className="text-lg font-medium text-accent-foreground">Account</h3>
                                <div className="space-y-4">
                                    <div>
                                        <label className="block text-sm font-medium mb-1">Username</label>
                                        <input type="text" className="w-full p-3 border border-border-brown rounded text-base bg-card text-card-foreground" />
                                    </div>
                                    <div>
                                        <label className="block text-sm font-medium mb-1">Email</label>
                                        <input type="email" className="w-full p-3 border border-border-brown rounded text-base bg-card text-card-foreground" />
                                    </div>
                                </div>
                            </div>

                            <div className="space-y-2">
                                <h3 className="text-lg font-medium text-accent-foreground">Preferences</h3>
                                <div className="space-y-3">
                                    <div className="flex items-center">
                                        <input type="checkbox" id="notifications" className="mr-3 h-4 w-4" />
                                        <label htmlFor="notifications" className="text-base">Enable notifications</label>
                                    </div>
                                </div>
                            </div>

                            <Button className="w-full bg-accent-foreground text-primary-foreground p-3 text-base mt-4">Save Changes</Button>
                        </div>
                    </div>
                </div>
            )}
        </>
    )
}