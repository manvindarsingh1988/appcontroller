import { useState }  from 'react';
import axios from 'axios';
import './UpdateDetails.css';
import URL from './url.json';

const Login = () => {
    const [userName, setUserName] = useState('');
    const [password, setPassword] = useState('');
    const [message, setMessage] = useState('');
    function handleLogin(e) {
        e.preventDefault();
        const payload = {
            'userName': userName,
            'password': password
        };
          axios.post(URL.url + 'appinfo/ValidateUser', payload)
          .then(response => {
            console.log(response.data);
            if(response.data) {
                localStorage.setItem('token', response.data);
                console.log(response.data);
                window.location.href = "/";
            } else {
                setMessage('Login Failed!')
            }
          })
          .catch(error => {
            if (error.response) {
              console.log(error.response.data);
              console.log(error.response.status);
              console.log(error.response.headers);
            } else if (error.request) {
              console.log(error.request);
            } else {
              console.log('Error', error.message);
            }
            console.log(error.config);
          });
    }

    return (
        <div className="popup">            
            <div className="popup-inner">    
                <div className='popup-inner-div'>
                    <p style={{color:'red'}}>{message}</p>
                    <form onSubmit={handleLogin}>
                        <label>
                            Name:
                            <input type="text" required value={userName} onChange={e => setUserName(e.target.value)} />
                        </label>
                        <label>
                            Password:
                            <input type="password" required value={password} onChange={e => setPassword(e.target.value)} />
                        </label>                        
                        <button type="submit">Log in</button>
                    </form>
                </div>                   
            </div>
            
        </div>
    )
};

export default Login;