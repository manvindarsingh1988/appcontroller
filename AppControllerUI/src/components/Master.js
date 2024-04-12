import { useState, useEffect }  from 'react';
import axios from 'axios';
import '../css/Master.css';
import URL from '../data/url.json';

const Master = () => {
    const [updatedOn, setUpdatedOn] = useState(0)
    const [checked, setChecked] = useState(false);
    const [validity, setValidity] = useState(0);
    const [apps, setApps] = useState([]);
    const [appVersion, setAppVersion] = useState('');
    const [formValues, setFormValues] = useState({
        name: '',
        type: 'URL',
        user: ''
      });
    useEffect(() => {
        axios.get(URL.url + 'appinfo/GetApplicationSettings')
        .then(res => {
            console.log(res.data);
            setChecked(res.data.killApps);
            setApps(res.data.allowedAppsAndUrls);
            setValidity(res.data.userValidity);
            setAppVersion(res.data.appVersion);
        }) 
        .catch(err => console.log(err));
       }, [updatedOn]);

    const handleChange = (e) => {

      if(window.confirm(e.target.checked ? 'Are you sure to change this flag, It will start killing apps on all linked systems?'
      :'Are you sure to change this flag, It will stop killing apps on all linked systems?')) {
        setChecked(e.target.checked);
        const payload = {
            killApp: e.target.checked,
          }
          
          axios.post(URL.url + "appinfo/AddKillAppSetting", payload, {
            headers: {
              'Content-Type': 'application/json'
            }
          })
          .then(res => {
        });
      }
        
    };

    const handleDelete = (id) => { 
      if(window.confirm('Are you sure to delete this app or URL, It will start killing this app or URL on all linked systems?')) {
        const payload = {
            id: id
          }        
          axios.post(URL.url + "appinfo/DeleteURLOrApp", payload, {
            headers: {
              'Content-Type': 'application/json'
            }
          })
          .then(res => {
            setUpdatedOn(new Date());
        });
      }
    };

    const handleNameAndType = (e) => { 
        const { name, value } = e.target;
        setFormValues({ ...formValues, [name]: value });
    };

    const handleSave = (e) => {
        if(formValues.name) {
            const payload = {
                name: formValues.name,
                type: formValues.type,
                user: formValues.user
              }
              
              axios.post(URL.url + "appinfo/AddURLOrApp", payload, {
                headers: {
                  'Content-Type': 'application/json'
                }
              })
              .then(res => {
                setUpdatedOn(new Date());
                setFormValues({
                  name: '',
                  type: 'URL',
                  user: ''
                });
            }) 
        }
        
    };

    const updateValidity = () => {
      if(validity) {
          const payload = {
              validity: validity
            }
            
            axios.post(URL.url + "appinfo/UpdateValidity", payload, {
              headers: {
                'Content-Type': 'application/json'
              }
            })
            .then(res => {
              alert('Active time updated successfully.')
              setUpdatedOn(new Date());
          }) 
      }
      
  };

    const st = {
        width: 'auto',
        border: '1px solid black',
        padding: '10px',
        margin: '20px'
      }
  
    return (
      <div style={{marginTop: "20px"}}>
        <label>
          <input
          style={{marginLeft: "20px"}}
            type="checkbox"
            checked={checked}
            onChange={handleChange}
          />
          Kill Apps
        </label>
        <div style={st}>
          <label style={{marginLeft: "10px"}}>
          User Active Check Time in Minutes:
                    <input style={{marginLeft: "5px"}} name='user'
                        type="text"
                        value={validity}
                        onChange={e => setValidity(e.target.value)}
                    />
                </label>
                <button style={{marginTop: "5px", marginBottom: "5px", marginLeft: "10px"}} onClick={updateValidity}>Update</button>
        </div>
        <div style={st}>
          <label style={{marginLeft: "10px"}}>
          Latest Application Version:
                    <b> {appVersion}</b>
                </label>
        </div>
        <p style={{marginLeft: "20px"}}><b>Allowed Apps and URLs:</b></p>
        <div style={st}>
            <div>
                <label>
                    Enter Url or App Name:
                    <input style={{marginLeft: "5px"}} name='name'
                        type="text"
                        value={formValues.name}
                        onChange={handleNameAndType}
                    />
                </label>
                <label style={{marginLeft: "10px"}}>
                    Enter User:
                    <input style={{marginLeft: "5px"}} name='user'
                        type="text"
                        value={formValues.user}
                        onChange={handleNameAndType}
                    />
                </label>
                <label style={{marginLeft: "10px"}} >
                    Pick a Type:
                    <select style={{marginLeft: "5px"}} id="selectedtype" name='type' onChange={handleNameAndType} value={formValues.type}>
                        <option value="URL">URL</option>
                        <option value="App">App</option>
                    </select>                    
                </label>
                <button style={{marginTop: "5px", marginBottom: "5px", marginLeft: "10px"}} onClick={handleSave}>Add</button> 
            </div>            
            
            
                <table>
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>User</th>
                            <th>Type</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {apps.map((item, index) => {
                            return (
                            <tr key={index}>
                                <td>{item.name}</td>
                                <td>{item.user}</td>
                                <td>{item.type}</td>
                                <td>
                                    <button onClick={() => handleDelete(item.id)} >Delete</button>
                                </td>
                            </tr>
                            );
                        })}
                    </tbody>
                </table>
            
        </div>
        
      </div>
    );
  };
  
  export default Master;