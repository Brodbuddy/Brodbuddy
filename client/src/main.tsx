import { createRoot } from 'react-dom/client'
import { BrowserRouter } from "react-router-dom"
import App from './App.tsx'
import './index.css'
import { SidebarProvider } from "./import"

createRoot(document.getElementById('root')!).render(
        <BrowserRouter>
            <SidebarProvider defaultOpen={false}>
                <App />
            </SidebarProvider>
        </BrowserRouter>
)