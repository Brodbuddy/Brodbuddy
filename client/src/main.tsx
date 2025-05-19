import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from "react-router-dom"
import App from './App.tsx'
import './index.css'
import { SidebarProvider } from "./import"

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <BrowserRouter>
            <SidebarProvider>
                <App />
            </SidebarProvider>
        </BrowserRouter>
    </StrictMode>
)