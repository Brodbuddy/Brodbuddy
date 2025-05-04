import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { AppRoutes } from '../helpers/appRoutes';

export default function CompleteLoginPage() {
    const [code, setCode] = useState('');
    const [email, setEmail] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const navigate = useNavigate();
    const { completeLogin } = useAuth();

    useEffect(() => {
        const storedEmail = sessionStorage.getItem('loginEmail');
        if (!storedEmail) {
            navigate(AppRoutes.login);
            return;
        }
        setEmail(storedEmail);
    }, [navigate]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsLoading(true);
        setError(null);

        try {
            const numericCode = parseInt(code, 10);
            if (isNaN(numericCode)) {
                setError('Please enter a valid numeric code');
                setIsLoading(false);
                return;
            }

            const success = await completeLogin(email, numericCode);
            if (!success) {
                setError('Invalid verification code. Please try again.');
            }
        } catch (err) {
            setError('An error occurred. Please try again later.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="login-container">
            <h1>Enter Verification Code</h1>
            <p>We sent a code to {email}. Please enter it below to complete login.</p>

            {error && <div className="error-message">{error}</div>}

            <form onSubmit={handleSubmit}>
                <div className="form-group">
                    <label htmlFor="code">Verification Code</label>
                    <input
                        type="text"
                        id="code"
                        value={code}
                        onChange={(e) => setCode(e.target.value)}
                        placeholder="Enter the code you received"
                        required
                        disabled={isLoading}
                    />
                </div>

                <button type="submit" disabled={isLoading}>
                    {isLoading ? 'Verifying...' : 'Login'}
                </button>

                <button
                    type="button"
                    className="secondary-button"
                    onClick={() => navigate(AppRoutes.login)}
                >
                    Back
                </button>
            </form>
        </div>
    );
};