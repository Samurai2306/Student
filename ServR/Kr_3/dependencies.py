from fastapi import Depends, HTTPException, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer

from security import decode_access_token

bearer_scheme = HTTPBearer(auto_error=False)


async def get_current_user_payload(
    creds: HTTPAuthorizationCredentials | None = Depends(bearer_scheme),
):
    if creds is None or creds.scheme.lower() != "bearer":
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Not authenticated",
            headers={"WWW-Authenticate": "Bearer"},
        )
    try:
        payload = decode_access_token(creds.credentials)
        username = payload.get("sub")
        role = payload.get("role")
        if not username or not role:
            raise ValueError("missing claims")
        return {"username": str(username), "role": str(role)}
    except Exception:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid or expired token",
            headers={"WWW-Authenticate": "Bearer"},
        )


def require_roles(*allowed: str):
    async def checker(user: dict = Depends(get_current_user_payload)):
        if user["role"] not in allowed:
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail="Insufficient permissions",
            )
        return user

    return checker
