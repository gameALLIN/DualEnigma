# Create test users and add root as friend
$base = "http://localhost:8081"
$users = @("testuser1","testuser2","testuser3","testuser4","testuser5")

function PostJson($Uri, $Body) {
    Invoke-RestMethod -Uri $Uri -Method Post -ContentType "application/json; charset=utf-8" -Body ($Body | ConvertTo-Json -Compress)
}

function GetErrBody($ErrorRecord) {
    $body = ""
    try { $stream = $ErrorRecord.Exception.Response.GetResponseStream(); $body = (New-Object IO.StreamReader($stream)).ReadToEnd() } catch {}
    return $body
}

# 1. Register test users (skip if exists)
foreach ($u in $users) {
    try {
        $r = PostJson "$base/api/auth/register" @{username=$u; password="123456"}
        Write-Output "[REG] $u OK accountId=$($r.accountId)"
    } catch {
        Write-Output "[REG] $u SKIP: $(GetErrBody $_)"
    }
}

# 2. Each test user sends friend request to root
foreach ($u in $users) {
    $login = PostJson "$base/api/auth/login" @{username=$u; password="123456"}
    $h = @{Authorization = "Bearer $($login.token)"}
    try {
        $r = Invoke-RestMethod -Uri "$base/api/friends/requests" -Method Post -Headers $h -ContentType "application/json; charset=utf-8" -Body (@{username="root"} | ConvertTo-Json -Compress)
        Write-Output "[REQ] $u -> root sent requestId=$($r.requestId)"
    } catch {
        Write-Output "[REQ] $u FAIL: $(GetErrBody $_)"
    }
}

# 3. Root logs in and accepts all pending requests
$root = PostJson "$base/api/auth/login" @{username="root"; password="123456"}
$rh = @{Authorization = "Bearer $($root.token)"}
$reqs = Invoke-RestMethod -Uri "$base/api/friends/requests" -Method Get -Headers $rh
foreach ($req in @($reqs)) {
    Invoke-RestMethod -Uri "$base/api/friends/requests/$($req.requestId)/accept" -Method Put -Headers $rh | Out-Null
    Write-Output "[ACC] root accepted $($req.fromUsername) (requestId=$($req.requestId))"
}

# 4. Verify root friend list
$friends = Invoke-RestMethod -Uri "$base/api/friends" -Method Get -Headers $rh
Write-Output "[VERIFY] root friends: $(@($friends) | ForEach-Object { $_.username })"
