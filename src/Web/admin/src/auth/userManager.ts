import { UserManager, WebStorageStateStore } from 'oidc-client-ts'
import { config } from '../config'

export const userManager = new UserManager({
  authority: config.authority,
  client_id: config.clientId,
  redirect_uri: config.redirectUri,
  post_logout_redirect_uri: config.postLogoutRedirectUri,
  response_type: 'code',
  scope: 'openid profile',
  automaticSilentRenew: true,
  userStore: new WebStorageStateStore({ store: window.localStorage }),
})
