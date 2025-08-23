use log::debug;
use named_pipe::PipeClient;
use rand::distr::{Alphanumeric, Distribution};
use std::{
    error::Error,
    fmt,
    io::{Read, Write},
};

#[derive(Debug, Clone)]
pub struct PipeError {
    pub message: String,
    pub is_timeout: bool,
}

impl Error for PipeError {}

impl fmt::Display for PipeError {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        write!(f, "{}", self.message)
    }
}

#[derive(Debug, Clone)]
pub struct ApiResponse {
    pub status_code: String,
    pub message: String,
    pub details: String,
}

impl From<String> for ApiResponse {
    fn from(string: String) -> Self {
        let mut parts = string.split("\nAdmApiDataRow=");
        let status_code = parts.next().unwrap_or("").to_string();
        let message = parts.next().unwrap_or("").to_string();
        let details = parts.next().unwrap_or("").to_string();
        ApiResponse {
            status_code,
            message,
            details,
        }
    }
}

pub fn send_message_and_get_reply(msg: &str, timeout: u32, channel: &str) -> Result<ApiResponse, PipeError> {
    let mut response_pipe_id = String::from("rust_");
    let mut rng = rand::rng();
    let unique_id: String = Alphanumeric.sample_iter(&mut rng).take(10).map(char::from).collect();
    response_pipe_id.push_str(&unique_id);
    send_message(msg, timeout, &channel.to_lowercase(), &response_pipe_id)?;
    return receive_reply(&response_pipe_id, timeout);
}

fn send_message(msg: &str, timeout: u32, request_channel: &str, response_channel: &str) -> Result<(), PipeError> {
    let mut request_pipe = connect_with_timeout(&format!("\\\\.\\pipe\\admpipe_request_{}", request_channel), timeout)?;

    let message = format!("{}\n{}", msg, response_channel);
    let duration = std::time::Duration::from_millis(timeout as u64);
    request_pipe.set_write_timeout(Some(duration));
    if let Err(e) = request_pipe.write_all(message.as_bytes()) {
        return Err(PipeError {
            message: format!("{}", e),
            is_timeout: false,
        });
    };
    Ok(())
}

fn receive_reply(response_channel: &str, timeout: u32) -> Result<ApiResponse, PipeError> {
    let mut response_pipe = connect_with_timeout(&format!("\\\\.\\pipe\\admpipe_response_{}", response_channel), timeout)?;
    let duration = std::time::Duration::from_millis(timeout as u64);
    response_pipe.set_read_timeout(Some(duration));
    let mut buf: Vec<u8> = Vec::new();
    if let Err(result) = response_pipe.read_to_end(&mut buf) {
        return Err(PipeError {
            message: format!("{}", result),
            is_timeout: false,
        });
    };
    let out = match String::from_utf8(buf) {
        Ok(converted) => converted,
        Err(e) => {
            return Err(PipeError {
                message: format!("{}", e),
                is_timeout: false,
            })
        }
    };

    Ok(out.into())
}

fn connect_with_timeout(address: &str, timeout: u32) -> Result<PipeClient, PipeError> {
    let mut pipe_connection_attempt = Err(PipeError {
        message: String::from("never connected"),
        is_timeout: true,
    });
    let retries = timeout / 100;
    for _ in 0..retries {
        pipe_connection_attempt = match PipeClient::connect_ms(address, timeout) {
            Ok(pipe) => Ok(pipe),
            Err(e) => {
                std::thread::sleep(std::time::Duration::from_millis(100 as u64));
                Err(PipeError {
                    message: format!("{}", e),
                    is_timeout: true,
                })
            }
        };
        if pipe_connection_attempt.is_ok() {
            debug!("connected!");
            break;
        }
    }
    pipe_connection_attempt
}

#[cfg(test)]
mod tests {
    use std::error::Error;

    use log::info;

    use crate::setup_logger;

    use super::*;

    #[test]
    fn test_message_capabilities() -> Result<(), Box<dyn Error>> {
        setup_logger()?;
        let response = send_message_and_get_reply("--alive", 5000, "sam")?;
        info!("{:?}", response);
        Ok(())
    }
}
